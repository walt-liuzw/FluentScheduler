using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beisen.Common.Context;
using Beisen.Common.HelperObjects;
using FluentScheduler;
using JobwsClient.Common;
using JobwsClient.Config;

namespace JobwsClient
{
    public static class JobProvider
    {
        private static string MakeRedisKey(int tenantId,string jobAppName)
        {
            return $"{jobAppName}_JobDataKey_{tenantId}";
        }

        private static string MakeJobValue(int operatorId, DateTime runtime, JobClient job)
        {
            job.Args = job.Args ?? ""; //把null处理为空字符串
            return $"{operatorId}_{runtime}_{job.GetType().AssemblyQualifiedName}_{job.Args}";
        }

        private static string MakeLockKey(int tenantId, string jobKey,string jobAppName)
        {
            return $"{jobAppName}_JobLockKey_{tenantId}_{jobKey}";
        }

        private static Tuple<int, DateTime, JobClient> GetJobValue(string jobValue)
        {
            if (string.IsNullOrEmpty(jobValue))
                return null;
            var infos = jobValue.Split('_');
            if (infos.Length < 4)
                return null;
            var tempUserId = infos[0].ToInt();
            var tempRunTime = infos[1].ToDateTime();
            if (!tempUserId.HasValue)
            {
                return null;
            }

            var jobType = Type.GetType(infos[2], false, false);
            if (jobType == null)
                throw new Exception($"GetJobValue未能加载对应类型：{infos[2]}");
            var job = Activator.CreateInstance(jobType) as JobClient;
            var prefix = $"{infos[0]}_{infos[1]}_{infos[2]}_";
            if (job != null)
            {
                var jobArg = jobValue.Substring(prefix.Length, jobValue.Length - prefix.Length);
                job.Args = jobArg;
            }

            return new Tuple<int, DateTime, JobClient>(tempUserId.Value, tempRunTime.Value, job);
        }

        public static bool AddJob(JobArgs args, bool fromRedis = false)
        {
            ArgumentHelper.AssertValuesPositive(args.TenantId);
            ArgumentHelper.AssertNotNullOrEmpty(args.JobKey);
            ArgumentHelper.AssertNotNullOrEmpty(args.JobAppName);
            ArgumentHelper.AssertNotNull(args.Job, "job must not null");

            string redisKey = MakeRedisKey(args.TenantId, args.JobAppName);
            string redisLockKey = MakeLockKey(args.TenantId, args.JobKey, args.JobAppName);

            //不是从redis加载的需要保存到redis，如果是从redis加载则不用再回写了
            if (!fromRedis)
            {
                string redisJobValue = MakeJobValue(args.OperatorId, args.RunTime, args.Job);
                RedisCacheHelper.SetHRedis(args.TenantId, redisKey, args.JobKey, redisJobValue);
            }

            JobManager.AddJob(
            () =>
            {
                //加分布式锁
                //获取Key 对应的Value。
                //拆Value，获取Runtime
                //比较runtime，如果在允许的时间差范围内，则执行。否则，return
                //执行完，删掉jobKey
                //释放分布式锁
                ApplicationContext.Current.TenantId = args.TenantId;
                ApplicationContext.Current.UserId = args.OperatorId;
                ApplicationContext.Current.ApplicationName = args.JobAppName;
                var lockRes = DistributeRedisLock.TryLock(args.TenantId, redisLockKey, "", args.LimitSeconds);
                if (!lockRes)
                {
                    LogHelper.Instance.Debug($"定时任务未执行，加锁失败。LockKey：{redisLockKey}，JobKey：{args.JobKey}");
                    return;
                }
                try
                {
                    var jobValue = RedisCacheHelper.GetHRedis(args.TenantId, redisKey, args.JobKey);
                    if (string.IsNullOrEmpty(jobValue))
                    {
                        LogHelper.Instance.Debug($"定时任务未执行，Key不存在。{args.JobKey}");
                        return;
                    }
                    var jobInfo = GetJobValue(jobValue);
                    if (jobInfo == null)
                    {
                        LogHelper.Instance.Debug($"定时任务未执行，JobInfo为空。JobKey:{args.JobKey}, JobValue:{jobValue}");
                        return;
                    }
                    //比如，一个Job的实际执行时间是12:00:00，Redis里存的是12:00:00，实际FluentScheduler也是12:00:00触发执行的。
                    //但是执行到比较时间这块，可能是12:00:05了（可能要排队、或者性能问题等），导致时间不匹配。因此，我们允许时间出现轻微误差
                    //但是，如果时间间隔超过该轻微误差（1分钟），则认为时间不匹配
                    if (Math.Abs((jobInfo.Item2 - DateTime.Now).TotalSeconds) >= 60)
                    {
                        LogHelper.Instance.Debug($"定时任务未执行，时间不匹配。JobKey:{args.JobKey}, JobValue:{jobValue}");
                        return;
                    }
                    args.Job.Execute();
                    RedisCacheHelper.DelHRedis(args.TenantId, redisKey, args.JobKey);
                    LogHelper.Instance.Debug($"执行定时任务成功。JobKey：{args.JobKey}，JobValue{jobValue}");
                }
                catch (Exception ex)
                {
                    LogHelper.Instance.Error($"执行定时任务异常。JobKey：{args.JobKey}，EX：{ex.Message}");
                }
                finally
                {
                    DistributeRedisLock.UnLock(args.TenantId, redisLockKey, "");
                }
            },
            (schedule) =>
            {
                RememberTenant(args.TenantId);
                schedule.WithName(args.JobKey).ToRunOnceAt(args.RunTime);
            });
            return true;
        }

        public static bool UpdateJob(JobArgs args)
        {
            return AddJob(args);
        }

        public static bool DeleteJob(int tenantId, string jobKey,string jobAppName)
        {
            string redisKey = MakeRedisKey(tenantId, jobAppName);
            return RedisCacheHelper.DelHRedis(tenantId, redisKey, jobKey) > 0;
        }
        public static void RememberTenant(int tenantId)
        {
            string redisAllTenantIdsSetKey = RedisConfig.TotalUseTimerTenantIds;
            int systemTenantId = RedisConfig.UPaasSystemTenantId;
            RedisCacheHelper.SAdd(systemTenantId, redisAllTenantIdsSetKey, tenantId.ToString());
        }
        public static void InitialJobs(string jobAppName)
        {
            string redisAllTenantIdsSetKey = RedisConfig.TotalUseTimerTenantIds;
            int systemTenantId = RedisConfig.UPaasSystemTenantId;
            var members = RedisCacheHelper.SMembers(systemTenantId, redisAllTenantIdsSetKey);
            if (members == null || members.Count == 0)
                return;
            members = members.Distinct().ToList();
            foreach (var member in members)
            {
                if (!member.ToInt().HasValue)
                    continue;
                var tenantId = member.ToInt().Value;
                string redisKey = MakeRedisKey(tenantId, jobAppName);
                var redisContents = RedisCacheHelper.GetHRedis(tenantId, redisKey);
                foreach (var kv in redisContents)
                {
                    var infos = GetJobValue(kv.Value);
                    if (infos == null || infos.Item2 < DateTime.Now)
                    {
                        RedisCacheHelper.DelHRedis(tenantId, redisKey, kv.Key);
                        continue;
                    }
                    //将redis的数据加载到服务器内存
                    AddJob(new JobArgs()
                    {
                        TenantId = tenantId,
                        OperatorId = infos.Item1,
                        JobKey = kv.Key,
                        RunTime = infos.Item2,
                        Job = infos.Item3,
                        JobAppName = jobAppName
                    }, true);
                }
            }
        }
    }


}
