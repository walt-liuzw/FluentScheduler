using Beisen.Logging;
using System;
using System.Diagnostics;
using Newtonsoft.Json;

namespace JobwsClient.Common
{
    /// <summary>
    /// 设计目的：减少代码侵入性，简化日志格式，增加可读性
    /// TODO：得想办法切入编程
    /// </summary>
    public sealed class LogHelper : ILogHelper
    {
        #region Singleton
        LogWrapper _log = null;
        private LogHelper()
        {
            _log = new LogWrapper();
        }
        public static readonly LogHelper Instance = new LogHelper();

        #endregion

        #region 日志api

        private void AddLog(string Msg, StackTrace stack = null, string Tittle = "", int TenantId = 0, int UserId = 0, LogType logtype = LogType.Debug,Object Context=null)
        {
            try
            {
                if (stack == null)
                    stack = new StackTrace(true);
                var methodBase = stack.GetFrame(1).GetMethod();
                Type stacktype = methodBase.DeclaringType;
                string fileName = stacktype.FullName;//文件名
                string methodName = methodBase.Name;//方法名
                var logModel = new LogModel
                {
                    TenantId = TenantId,
                    UserId = UserId,
                    Message = Msg,
                    Title = Tittle,
                    MethodName = methodName,
                    FileName = fileName,
                    CodeLine = "0"
                };
                var jsonValue = JsonConvert.SerializeObject(logModel);
                switch (logtype)
                {
                    case LogType.Debug:
                        _log.Debug(jsonValue);
                        break;
                    case LogType.Info:
                        _log.Info(jsonValue);
                        break;
                    case LogType.Warn:
                        _log.Warn(jsonValue);
                        break;
                    case LogType.Error:
                        _log.Error(jsonValue);
                        break;
                    case LogType.Fatal:
                        _log.Fatal(jsonValue);
                        break;
                    default:
                        _log.Debug(jsonValue);
                        break;
                }
            }
            catch
            {
            }
        }
        private void AddException(string Msg, StackTrace stack = null, string Tittle = "", int TenantId = 0, int UserId = 0, LogType logtype = LogType.Error, Exception ex = null,Object Context=null)
        {
            try
            {
                if (stack == null)
                    stack = new StackTrace(true);
                if (ex == null)
                    ex = new Exception("未注明异常类型");
                var methodBase = stack.GetFrame(1).GetMethod();
                Type stacktype = methodBase.DeclaringType;
                string fileName = stacktype.FullName;//文件名
                string methodName = methodBase.Name;//方法名
                string codeLine = "0";
                try
                {
                    codeLine = ex.StackTrace.Substring(ex.StackTrace.IndexOf("行号"), ex.StackTrace.Length - ex.StackTrace.IndexOf("行号"));
                }
                catch
                {
                }
                var logModel = new LogModel
                {
                    TenantId = TenantId,
                    UserId = UserId,
                    Message = Msg,
                    Title = Tittle,
                    MethodName = methodName,
                    FileName = fileName,
                    CodeLine = codeLine,
                    Ex=ex
                };
                var jsonValue = JsonConvert.SerializeObject(logModel);
                switch (logtype)
                {
                    case LogType.Debug:
                        _log.Debug(jsonValue, ex);
                        break;
                    case LogType.Info:
                        _log.Info(jsonValue, ex);
                        break;
                    case LogType.Warn:
                        _log.Warn(jsonValue, ex);
                        break;
                    case LogType.Error:
                        _log.Error(jsonValue, ex);
                        break;
                    case LogType.Fatal:
                        _log.Fatal(jsonValue, ex);
                        break;
                    default:
                        _log.Debug(jsonValue, ex);
                        break;
                }
            }
            catch
            {
            }
        }

        /// <summary>
        /// 自助断点日志
        /// </summary>
        /// <param name="Msg">开头附加描述信息</param>
        /// <param name="logtype">日志级别，可以根据LogType枚举类型来获取</param>
        /// <param name="ex">参数，可不传，抛错堆栈</param>
        public void Dump(string Msg, LogType logtype = LogType.Debug, Exception ex = null, string Tittle = "", int TenantId = 0, int UserId = 0,Object Context=null)
        {
            if (ex == null)
                AddLog(Msg, new StackTrace(true), Tittle, TenantId, UserId, logtype, Context);
            else
                AddException(Msg, new StackTrace(true), Tittle, TenantId, UserId, logtype, ex, Context);
        }

        #endregion

        #region 快速Dump接口
        public void Error(string msg)
        {
            AddException(Msg: msg);
        }
        public void Error(string msg,Exception ex)
        {
            AddException(Msg: msg, ex: ex);
        }
        public void Debug(string msg)
        {
            AddLog(msg, logtype: LogType.Debug);
        }
        public void Debug(string msg, int tenantId, int userId)
        {
            AddLog(msg, TenantId: tenantId, UserId: userId, logtype: LogType.Debug);
        }
        public void Warn(string msg)
        {
            AddLog(msg, logtype: LogType.Warn);
        }
        #endregion
    }

}