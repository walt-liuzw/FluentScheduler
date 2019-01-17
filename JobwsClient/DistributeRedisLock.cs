using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Beisen.RedisV2.Provider;
using JobwsClient.Common;
using JobwsClient.Config;
using Newtonsoft.Json;

namespace JobwsClient
{
    public static class RedisCacheHelper
    {
        static readonly string RedisCacheKeyspace = RedisConfig.RedisKeyspace;
        public static int DelHRedis(int tenantId, string key, string field)
        {
            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    byte[] fieldBytes = Encoding.Default.GetBytes(field);
                    return redis.HDel(key, fieldBytes);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error($"在redis-Bytes删除[{key}]异常,Field:{field}", ex);
                return 0;
            }
        }
        public static byte[][] GetHRedisValueByKey(int tenantId, string key)
        {
            byte[][] result = new byte[][] { };
            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    //在redis查询
                    result = redis.HGetAll(key);
                }
            }
            catch (Exception ex)
            {
                //进行查询异常操作
                LogHelper.Instance.Error($"在redis查询[{key}]异常", ex);
                //抛出异常
                throw new Exception(key, ex);
            }
            return result;
        }
        public static Dictionary<string, string> GetHRedis(int tenantId, string key)
        {
            //返回的bytes，是Key、value交替出现的List
            var bytes = GetHRedisValueByKey(tenantId, key);
            Dictionary<string, string> result = new Dictionary<string, string>();
            string tempKey = string.Empty;
            for (int i = 0; i < bytes.Length; i++)
            {
                var content = BytesToString(bytes[i]);
                if (i % 2 == 0)
                {//偶数项，代表Key
                    tempKey = content;
                }
                else
                {//奇数项，代表Value
                    if (result.ContainsKey(tempKey))
                        result[tempKey] = content;
                    else
                        result.Add(tempKey, content);
                }
            }
            return result;
        }
        public static string GetHRedis(int tenantId, string key, string field)
        {

            byte[] result = new byte[] { };
            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    //在redis查询
                    byte[] fieldByte = Encoding.Default.GetBytes(field);
                    result = redis.HGet(key, fieldByte);
                }
            }
            catch (Exception ex)
            {
                //进行查询异常操作
                LogHelper.Instance.Error($"在redis查询[{key}]异常", ex);
                //抛出异常
                throw new Exception(key, ex);
            }
            return BytesToString(result);
        }
        public static bool SetHRedis(int tenantId, string key, string field, string value, long timespan = 0)
        {
            bool result = false;
            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    //在redis查询
                    byte[] fieldByte = Encoding.Default.GetBytes(field);
                    byte[] valueByte = Encoding.Default.GetBytes(value);
                    result = redis.HSet(key, fieldByte, valueByte);
                    if (timespan > 0)
                        redis.ExpireAt(key, timespan);
                }
            }
            catch (Exception ex)
            {
                //进行查询异常操作
                LogHelper.Instance.Error($"在redis查询[{key}]异常", ex);
                //抛出异常
                throw new Exception(key, ex);
            }
            return true;
        }
        public static void SAdd(int tenantId, string key, string value)
        {
            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    byte[] members = Encoding.Default.GetBytes(value);
                    redis.SAdd(key, members);
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error($"在redis-SAdd创建[{key}]:[{value}]异常", ex);
                throw;
            }
        }
        #region Pipeline
        public static void PipelineSet<T>(int tenantId, string key, T value, Action onSuccess = null, Action<Exception> onException = null)
        {
            try
            {
                using (var redis = new RedisNativePipelineProviderV2(RedisCacheKeyspace, tenantId))
                {
                    var serializedValue = JsonConvert.SerializeObject(value);
                    redis.Set(key, StringToBytes(serializedValue), onSuccess, onException);
                    redis.Flush();
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error(string.Format("在redis创建[{0}]:[{1}]异常", key, value), ex);
                throw;
            }
        }

        #endregion
        private static byte[] StringToBytes(string value)
        {
            if (value == null)
                return null;

            return Encoding.Default.GetBytes(value);
        }
        private static string BytesToString(byte[] value)
        {
            if (value == null)
                return null;

            return Encoding.Default.GetString(value);
        }
        private static List<string> ByteArrayToList(byte[][] bytes)
        {
            List<string> result = new List<string>();
            for (int i = 0; i < bytes.Length; i++)
            {
                var content = BytesToString(bytes[i]);
                result.Add(content);
            }

            return result;
        }
        public static List<string> SMembers(int tenantId, string key)
        {
            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    return ByteArrayToList(redis.SMembers(key));
                }
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error($"在redis-ScanMembers查询[{key}]异常", ex);
                throw;
            }
            throw new NotImplementedException();
        }
    }

    public static class DistributeRedisLock
    {
        static readonly string RedisCacheKeyspace = RedisConfig.RedisKeyspace;
        #region DistributeRedisLock
        /// <summary>
        /// 设置锁
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="seconds"></param>
        /// <returns></returns>
        public static bool TryLock(int tenantId, string key, string value, int seconds = 20)
        {
            //申请成功标志
            bool success = false;

            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    //redis中申请key-value
                    success = redis.Set(key, StringToBytes(value), Beisen.RedisV2.Enums.SetExpireType.EX, seconds, Beisen.RedisV2.Enums.SetKeyType.NX);
                }
            }
            catch (Exception ex)
            {
                //进行创建异常的操作
                LogHelper.Instance.Error($"在redis创建[{key}]:[{value}]异常", ex);
                success = false;
                throw ex;
            }
            finally
            {
                //进行创建成功或失败的操作
                LogHelper.Instance.Debug($"在redis创建[{key}]:[{value}]{(success ? "成功" : "失败")}");
            }

            return success;
        }
        /// <summary>
        /// 解锁
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static bool UnLock(int tenantId, string key, string value)
        {
            //删除成功标志
            bool success = false;
            //异常出现次数计数
            int exceptionTimes = 0;
            while (!success)
            {
                //进行解锁尝试
                try
                {
                    //判断锁是否存在（且为自身所加锁）
                    if (RedisExist(tenantId, key))
                    {
                        using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                        {
                            //redis中删除key
                            int affected = redis.Del(key);

                            //影响行数大于0，则删除成功
                            if (affected > 0)
                            {
                                //进行key-value删除成功操作
                                LogHelper.Instance.Debug($"在redis删除[{key}]:[{value}]成功，共{affected}条");
                                success = true;
                            }
                            else
                                throw new Exception(key, new Exception("删除操作影响0条，删除失败"));
                        }
                    }
                    else
                    {
                        //进行key-value不存在操作
                        LogHelper.Instance.Debug($"在redis删除[{key}]:[{value}]成功，记录不存在，无需删除");
                        success = true;
                    }
                }
                catch (Exception ex)
                {
                    //进行删除异常的操作
                    exceptionTimes++;
                    LogHelper.Instance.Error($"在redis删除[{key}]:[{value}]异常", ex);
                    success = false;

                    //异常次数超出限制，抛出异常
                    if (exceptionTimes >= 3)
                        throw new Exception(key, ex);
                }
            }
            return success;
        }
        /// <summary>
        /// 查询锁
        /// </summary>
        /// <param name="tenantId"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        private static bool RedisExist(int tenantId, string key)
        {
            //查询成功标志
            bool success = false;

            try
            {
                using (var redis = new RedisNativeProviderV2(RedisCacheKeyspace, tenantId))
                {
                    //在redis查询
                    success = redis.Exists(key);
                }
            }
            catch (Exception ex)
            {
                //进行查询异常操作
                LogHelper.Instance.Error($"在redis查询[{key}]异常", ex);
                success = false;
                //抛出异常
                throw new Exception(key, ex);
            }
            finally
            {
                //进行查询成功或失败操作
                LogHelper.Instance.Debug($"在redis查询[{key}]{(success ? "存在" : "不存在")}");
            }

            return success;
        }
        #endregion

        private static byte[] StringToBytes(string value)
        {
            if (value == null)
                return null;

            return Encoding.Default.GetBytes(value);
        }
    }
}
