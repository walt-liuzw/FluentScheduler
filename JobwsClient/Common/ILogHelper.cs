using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace JobwsClient.Common
{
    public interface ILogHelper
    {
        void Dump(string Msg, LogType logtype = LogType.Debug, Exception ex = null, string Tittle = "", int TenantId = 0, int UserId = 0,Object Context=null);
        void Error(string msg);
        void Debug(string msg);
        void Warn(string msg);
    }
    public class LogModel
    {
        public LogModel()
        {
            Id = Guid.NewGuid();
            CreateTime = DateTime.Now;
        }

        private Guid Id { get; }

        public string FileName { get; set; }
        public string MethodName { get; set; }
        public string CodeLine { get; set; }
        public string Title { get; set; }
        public string Message { get; set; }
        public int TenantId { get; set; }
        public int UserId { get; set; }
        private DateTime CreateTime { get; }
        public Exception Ex { get; set; }
    }
    public enum LogType
    {
        Debug = 0,
        Info,
        Warn,
        Error,
        Fatal
    }
}
