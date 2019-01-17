using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FluentScheduler;

namespace JobwsClient
{
    public abstract class JobClient : IJob
    {
        public string Args { get; set; }
        //框架会把 字符串 赋值给Args。至于Args的解析，是反序列化，还是直接存Id、还是用逗号分隔，这个框架不管。具体实现类自行处理
        //注意，框架不能区分 null 和 ""，统一返回处理为 ""
        public abstract void Execute();
    }

    public class JobArgs
    {
        /// <summary>
        /// 租户ID
        /// </summary>
        public int TenantId { get; set; }
        /// <summary>
        /// 操作人
        /// </summary>
        public int OperatorId { get; set; }
        /// <summary>
        /// Job ID：一般用业务数据的主键
        /// </summary>
        public string JobKey { get; set; }
        /// <summary>
        /// 执行时间
        /// </summary>
        public DateTime RunTime { get; set; }
        /// <summary>
        /// Job实例
        /// </summary>
        public JobClient Job { get; set; }
        /// <summary>
        /// 锁定执行秒数
        /// </summary>
        public int LimitSeconds { get; set; } = 5;

        public string JobAppName { get; set; }
    }
}
