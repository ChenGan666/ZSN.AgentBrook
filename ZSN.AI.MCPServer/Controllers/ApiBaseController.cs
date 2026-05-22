using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.PIC;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Service.WebHelpers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using ZSN.AI.MCPServer.Attributes;

namespace ZSN.AI.MCPServer.Controllers
{
    [ApiRecoder]
    public class ApiBaseController : CommonApiBaseController
    {
        public string dataCode = "";
        public ApiBaseController()
        {

            
        }

        /// <summary>
        /// 记录API调用日志（兼容MCP和直接HTTP调用）
        /// </summary>
        /// <param name="requestParams">请求参数对象</param>
        /// <param name="response">响应结果对象</param>
        protected void LogApiCall(object requestParams, object response)
        {
            try
            {
                // 尝试从Cotroller的HttpContext或HttpContextHelper获取请求信息
                var httpContext = this.HttpContext ?? HttpContextHelper.Current;
                
                string url = "MCP";
                string method = "MCP";
                
                if (httpContext != null)
                {
                    url = httpContext.Request?.Path.Value ?? "MCP";
                    method = httpContext.Request?.Method ?? "MCP";
                }
                
                var log = new
                {
                    url = url,
                    method = method,
                    requestParams = requestParams,
                    response = response,
                    timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                };

                DefaultLogService.AddOperationLog(46, JsonConvert.SerializeObject(log, Formatting.Indented));
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"[LogApiCall] 记录日志失败: {ex.Message}");
            }
        }
    }
}
