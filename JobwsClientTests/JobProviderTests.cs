using Microsoft.VisualStudio.TestTools.UnitTesting;
using JobwsClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JobwsClient.Common;

namespace JobwsClient.Tests
{
    [TestClass()]
    public class JobProviderTests
    {
        [TestMethod()]
        public void AddJobTest()
        {
            MobileContent mobileContent = new MobileContent()
            {
                TenantId = 100002,
                UserId = 112664957,
                ToUserId = 112664957,
                Content = "hello",
                OId = "6cd57b3b-dab9-44f8-be16-9ae974b8e523",
                RunTime = new DateTime(2019, 1, 17, 20, 0, 0)
            };

            JobArgs args = new JobArgs()
            {
                TenantId = mobileContent.TenantId,
                OperatorId = mobileContent.UserId,
                JobKey = mobileContent.OId,
                Job = new TestJob(){ Args = mobileContent.ToJson() },
                RunTime = mobileContent.RunTime,
                LimitSeconds = 10,
                JobAppName = "UPaaSDemo"
            };
            JobProvider.AddJob(args);
            Assert.Fail();
        }

        [TestMethod()]
        public void UpdateJobTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void DeleteJobTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void RememberTenantTest()
        {
            Assert.Fail();
        }

        [TestMethod()]
        public void InitialJobsTest()
        {
            JobProvider.InitialJobs("UPaaSDemo");
            Assert.Fail();
        }
    }

    public class TestJob : JobClient
    {
        public override void Execute()
        {
            try
            {
                var mobileContent = Args.ToObject<MobileContent>();
                if (mobileContent == null)
                {
                    LogHelper.Instance.Error($"AddScheduleJob反序列化数据：{Args}  失败！");
                    return;
                }
                LogHelper.Instance.Debug($"MobilePush--租户{mobileContent.TenantId}，jobKey：{mobileContent.OId} 开始推送");
            }
            catch (Exception ex)
            {
                LogHelper.Instance.Error($"AddScheduleJob 执行异常：{ex}");
            }
        }
    }

    public class MobileContent
    {
        public int TenantId { get; set; }

        public int UserId { get; set; }

        public int ToUserId { get; set; }

        public string Content { get; set; }

        public string OId { get; set; }

        public DateTime RunTime { get; set; }
    }
}