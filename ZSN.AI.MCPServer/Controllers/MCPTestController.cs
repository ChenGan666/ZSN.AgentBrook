using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace ZSN.AI.MCPServer.Controllers
{
    [McpServerToolType]
    [ApiController]
    [Route("[controller]")]
    public class MCPTestController : ApiBaseController
    {
        private readonly ILogger<MCPTestController> _logger;

        public MCPTestController(ILogger<MCPTestController> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 测试MCP服务是否正常工作，并验证参数传递
        /// </summary>
        /// <param name="message">测试消息，字符串类型</param>
        /// <param name="number">测试数字，整数类型</param>
        /// <param name="flag">测试标志，布尔类型</param>
        /// <returns>包含输入参数回显和随机信息的响应</returns>
        [McpServerTool]
        [HttpGet(Name = "MCPTest")]
        [Description(@"测试MCP服务是否正常工作，并验证参数传递是否成功。
参数说明：
- message (可选): 测试消息，字符串类型，默认值为'Hello MCP'
- number (可选): 测试数字，整数类型，默认值为100
- flag (可选): 测试标志，布尔类型，默认值为true

返回值将包含输入参数的回显以及服务器生成的随机信息，用于验证MCP服务的完整性。")]
        public IActionResult MCPTest(
            [FromQuery] string message = "Hello MCP",
            [FromQuery] int number = 100,
            [FromQuery] bool flag = true)
        {
            var response = new
            {
                Success = true,
                Message = "测试MCP成功",
                InputParams = new
                {
                    Message = message,
                    Number = number,
                    Flag = flag,
                    ReceivedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff")
                },
                GeneratedData = new
                {
                    RequestId = Guid.NewGuid().ToString(),
                    Timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"),
                    RandomNumber = Random.Shared.Next(1000, 9999),
                    ServerTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
                }
            };

            _logger.LogInformation($"MCP测试请求 - RequestId: {response.GeneratedData.RequestId}, 输入参数: message={message}, number={number}, flag={flag}");

            // 记录API调用日志（兼容MCP和直接HTTP调用）
            LogApiCall(new { message, number, flag }, response);

            return Ok(response);
        }

    }
}
