using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Text;
using ZSN.Utils.Core.DI;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.MemoryQueue;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Extensions;
using Newtonsoft.Json;
using ZSN.AI.Service.WebHelpers;
using ZSN.AI.Entity;
using ZSN.AI.BLL;

namespace ZSN.AI.Service.WebHelpers
{
    public static class DefaultLogService
    {
        private static readonly string OperaterQueueName = "LogQueue_Data";

        private static IHttpContextAccessor ContextAccessor => ServiceLocator.GetInstance<IHttpContextAccessor>();

        static DefaultLogService()
        {
            QueueManager.CreateQueue<LogRecord>(OperaterQueueName, ConsumeOperateQueue);
            NLogHelper.WriteInfo("LogQueueProvider创建内存队列成功");
        }

        /// <summary>
        ///     获取操作记录的异步任务队列
        /// </summary>
        private static AsyncQueue<LogRecord> OperateQueue
        {
            get { return QueueManager.GetQueue<LogRecord>(OperaterQueueName); }
        }

        private static void ConsumeOperateQueue(List<LogRecord> lstInfo)
        {
            foreach (var record in lstInfo)
            {
                try
                {
                    var logMark = LogMarkBusiness.GetModel(record.MarkId);
                    if (logMark != null)
                    {
                        var logLevel = LogLevelBusiness.GetModel(logMark.LevelId);
                        record.LevelId = logLevel.Id;
                        if (logLevel.Status && logMark.Status)
                            LogRecordBusiness.Add(record);
                    }
                }
                catch (Exception ex)
                {
                    NLogHelper.WriteException("ConsumeOperateQueue:" + JsonConvert.SerializeObject(record), ex);
                }
            }
        }

        /// <summary>
        /// 用户操作记录
        /// </summary>
        /// <param name="markId"></param>
        /// <param name="logRemarks"></param>
        /// <param name="logRemarks"></param>
        /// <param name="uid"></param>
        public static void AddOperationLog(int markId, string logDetail, string logRemarks = "", string uid = null)
        {
            var ip = ContextAccessor?.HttpContext?.GetClientUserIp() ?? "127.0.0.1";
            var url = ContextAccessor?.HttpContext?.Request?.AbsoluteUri() ?? "";
            var userId = uid;
            if (uid == null)
            {

            }

            OperateQueue.PutMessage(
                new LogRecord()
                {
                    LogDetail = logDetail,
                    LogCreatorIP = ip,
                    LogRemarks = logRemarks,
                    LogUrl = url,
                    LogCreatorId = userId,
                    DateCode = DateTime.Now.ToDateCode(),
                    OperateTime = DateTime.Now,
                    CreateTime = DateTime.Now,
                    UpdateTime = DateTime.Now,
                    MarkId = markId
                });
        }

        public static void AddOperationLog(int typeId, Exception ex, string logDetail = "", string logRemarks = "")
        {
            AddOperationLog(typeId, BuildMessage(logDetail, ex), logRemarks);
        }

        private static string BuildMessage(string info, Exception ex = null)
        {
            var sb = new StringBuilder();
            var context = ServiceLocator.GetInstance<IHttpContextAccessor>()?.HttpContext;
            var request = context?.Request;
            sb.AppendFormat("============================\r\nTime:{0:yyyy-MM-dd HH:mm:ss.fff}\r\n{1}\r\n", DateTime.Now, info);

            if (request != null)
            {
                sb.AppendFormat("Url:{0}\r\n", request.GetDisplayUrl());
                if (null != request.UrlReferrer())
                {
                    sb.AppendFormat("UrlReferrer:{0}\r\n", request.UrlReferrer());
                }
                sb.AppendFormat("UserHostAddress:{0};{1};{2}\r\n",
                    request.HttpContext.GetClientUserIp(),
                    request.Headers["X-Forwarded-For"],
                    request.Headers["HTTP_NDUSER_FORWARDED_FOR_HAPROXY"]
                );
                //request.ServerVariables["LOCAL_ADDR"]);
            }

            if (ex != null)
            {
                if (ex is SqlException sqlEx)
                    sb.AppendFormat("Database:{0}\r\n", sqlEx.Server);
                sb.AppendFormat("Exception:{0}\r\n", ex);
            }
            sb.AppendLine();
            return sb.ToString();
        }
    }
}