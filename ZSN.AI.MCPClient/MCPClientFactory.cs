using Microsoft.Extensions.Logging;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;
using System;
using System.Net;
using System.Threading.Tasks;
using ZSN.AI.Entity;

namespace ZSN.AI.MCPClient
{
    /// <summary>
    /// MCP客户端工厂类，用于创建不同连接方式的客户端
    /// </summary>
    public static class MCPClientFactory
    {
        /// <summary>
        /// 创建MCP客户端实例
        /// </summary>
        /// <param name="config">MCP客户端配置</param>
        /// <param name="logger">日志记录器（可选）</param>
        /// <returns>MCP客户端接口</returns>
        public static async Task<IMCPClient> CreateAsync(MCPConfig config, ILogger? logger = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }
            try
            {
                var handler = new HttpClientHandler
                {
                    ServerCertificateCustomValidationCallback = (message, cert, chain, errors) => true
                };
                var client = new HttpClient(handler);
                System.Net.ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls13;
                System.Net.ServicePointManager.ServerCertificateValidationCallback = (sender, certificate, chain, sslPolicyErrors) => true;

                switch (config.Type)
                {
                    case MCPConnectionType.Http:
                        // 使用ModelContextProtocol的SSE实现作为HTTP连接的基础
                        if (string.IsNullOrEmpty(config.Url))
                        {
                            throw new ArgumentException("使用HTTP连接方式时必须指定Url", nameof(config.Url));
                        }
                        
                        var httpTransport = new SseClientTransport(
                        new SseClientTransportOptions
                        {
                            Name = config.ClientName,
                            Endpoint = new Uri(config.Url),
                            ConnectionTimeout = TimeSpan.FromSeconds(config.Timeout),
                            AdditionalHeaders = config.Headers,
                        },
                        client,  // 传入自定义的HttpClient
                        null,    // loggerFactory参数
                        false);  // ownsHttpClient参数，设为false以避免重复释放

                        var httpMcpClient = await McpClientFactory.CreateAsync(httpTransport);
                        return new MCPHttpClient(httpMcpClient, config, logger);

                    case MCPConnectionType.Sse:
                        // 使用ModelContextProtocol的SSE实现
                        if (string.IsNullOrEmpty(config.Url))
                        {
                            throw new ArgumentException("使用SSE连接方式时必须指定SseUrl", nameof(config.Url));
                        }

                        var sseTransport = new SseClientTransport(
                            new SseClientTransportOptions
                            {
                                Name = config.ClientName,
                                Endpoint = new Uri(config.Url),
                                ConnectionTimeout = TimeSpan.FromSeconds(config.Timeout),
                                AdditionalHeaders = config.Headers,
                            },
                            client,  // 传入自定义的HttpClient
                            null,    // loggerFactory参数
                            false);  // ownsHttpClient参数，设为false以避免重复释放

                        var sseMcpClient = await McpClientFactory.CreateAsync(sseTransport);
                        return new MCPSseClient(sseMcpClient, config, logger);


                    case MCPConnectionType.Stdio:
                        // 使用ModelContextProtocol的Stdio实现
                        if (string.IsNullOrEmpty(config.Command))
                        {
                            throw new ArgumentException("使用Stdio连接方式时必须指定Command", nameof(config.Command));
                        }

                        var stdioTransport = new StdioClientTransport(new StdioClientTransportOptions
                        {
                            Name = config.ClientName,
                            Command = config.Command,
                            Arguments = config.Args,
                            EnvironmentVariables = config.Env,
                            ShutdownTimeout = TimeSpan.FromSeconds(config.Timeout),
                        });

                        var stdioMcpClient = await McpClientFactory.CreateAsync(stdioTransport);
                        return new MCPStdioClient(stdioMcpClient, config, logger);

                    default:
                        throw new ArgumentException($"不支持的连接类型: {config.Type}");
                }
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException("创建MCP客户端失败", ex);
            }
        }
    }
}
