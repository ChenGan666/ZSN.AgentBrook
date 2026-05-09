
using Amazon.S3.Model;
using DocumentFormat.OpenXml.Drawing;
using DocumentFormat.OpenXml.Wordprocessing;
using LLama;
using log4net.Plugin;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.KernelMemory;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using Microsoft.SemanticKernel.Plugins.Core;
using Microsoft.SemanticKernel.TextGeneration;
using ModelContextProtocol.Client;
using OpenAI;
using OpenAI.Embeddings;
using OpenAI.Responses;
using RestSharp;
using System;
using System.ClientModel;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using ZSN.AI.BLL;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.MCPClient;
using ZSN.AI.Plugins;
using ZSN.Utils.Core.Extensions;
using ServiceLifetime = ZSN.AI.Core.Common.DependencyInjection.ServiceLifetime;

namespace ZSN.AI.Core.Service
{
    [ServiceDescription(typeof(IKernelService), ServiceLifetime.Scoped)]
    public class KernelService : IKernelService
    {
        private readonly FunctionService _functionService;
        private readonly IServiceProvider _serviceProvider;
        private Kernel _kernel;

        public KernelService(
              FunctionService functionService,
              IServiceProvider serviceProvider)
        {
            _functionService = functionService;
            _serviceProvider = serviceProvider;
        }

        /// <summary>
        /// 获取kernel实例，依赖注入不好按每个用户去 Import不同的插件，所以每次new一个新的kernel
        /// </summary>
        /// <param name="ModelInfo">模型信息</param>
        /// <returns>配置好的Kernel实例</returns>
        public Kernel GetKernel(LargeModelInfo ModelInfo)
        {
            try
            {
                // 确保模型信息有效
                if (ModelInfo == null)
                {
                    // 如果模型信息为空，则尝试使用默认的全局服务
                    return _serviceProvider.GetService<Kernel>() ?? CreateDefaultKernel();
                }
                
                var chatHttpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(ModelInfo.EndPoint);

                var builder = Kernel.CreateBuilder();

                OpenAIPromptExecutionSettings settings = new OpenAIPromptExecutionSettings();

                
                // 配置服务（图像模型也需要注册基础服务以保证Kernel可用）
                WithTextGenerationByAIType(builder, ModelInfo, chatHttpClient);

                // 注册基础插件
                RegisterPluginsWithBase(builder);
                
                // 构建并返回新的Kernel实例
                _kernel = builder.Build();
                return _kernel;
            }
            catch (Exception ex)
            {
                // 记录错误并返回默认Kernel
                return CreateDefaultKernel();
            }
        }

        /// <summary>
        /// 通过模型ID获取Kernel实例
        /// </summary>
        /// <param name="modelid">模型ID</param>
        /// <returns>配置好的Kernel实例</returns>
        public Kernel GetKernelByAIModelID(int modelid)
        {
            // 获取模型信息
            LargeModelInfo ModelInfo = LargeModelInfoBussiness.GetModel(modelid);
            if (ModelInfo == null)
            {
                // 如果没有找到模型，使用默认Kernel
                return CreateDefaultKernel();
            }

            return GetKernel(ModelInfo);
        }
        
        /// <summary>
        /// 创建一个默认的Kernel实例，当其他方法失败时使用
        /// </summary>
        /// <returns>默认配置的Kernel实例</returns>
        private Kernel CreateDefaultKernel()
        {
            // 不应该创建无效的Kernel,而是抛出明确的异常
            throw new InvalidOperationException(
                "无法创建Kernel实例：模型配置缺失或无效。" +
                "请确保已正确配置模型信息，包括 ModelName、ApiKey 和 EndPoint。" +
                "对于 Claw AI 节点，请检查主模型(model)或专用模型(planningModel等)的配置是否完整。");
        }

        private void WithTextGenerationByAIType(IKernelBuilder builder, LargeModelInfo chatModel, HttpClient chatHttpClient)
        {
            // 确保为所有情况注册IChatCompletionService
            switch (chatModel.ModelOrganizationID)
            {
                case ZSN.AI.Entity.Model.Enum.AIType.OpenAI:
                    builder.AddOpenAIChatCompletion(
                       modelId: chatModel.ModelName,
                       apiKey: chatModel.ModelKey,
                       httpClient: chatHttpClient);
                    break;
                case ZSN.AI.Entity.Model.Enum.AIType.Ollama:
                    builder.AddOpenAIChatCompletion(
                       modelId: chatModel.ModelName,
                       apiKey: chatModel.ModelKey,
                       httpClient: chatHttpClient);
                    break;
                case Entity.Model.Enum.AIType.Bigmodel:
                    builder.AddOpenAIChatCompletion(
                       modelId: chatModel.ModelName,
                       apiKey: chatModel.ModelKey,
                       httpClient: chatHttpClient);
                    break;
                default:
                    // 默认情况下也使用OpenAI兼容接口，确保始终注册IChatCompletionService
                    builder.AddOpenAIChatCompletion(
                       modelId: chatModel.ModelName,
                       apiKey: chatModel.ModelKey,
                       httpClient: chatHttpClient);
                    break;
            }
        }

        private static JsonObject BuildSimpleParamsSchema()
        {
            JsonObject props = new JsonObject
            {
                ["input"] = new JsonObject
                {
                    ["type"] = "string",
                    ["description"] = "自由文本输入"
                }
            };

            JsonObject parameters = new JsonObject
            {
                ["type"] = "object",
                ["properties"] = props
            };

            return parameters;
        }
        /// <summary>
        /// 将MCP客户端工具转换为Kernel函数，并导入到Kernel中
        /// </summary>
        /// <param name="_kernel"></param>
        /// <param name="clientTools"></param>
        public void ImportFunctions(Kernel _kernel, IList<McpClientTool> clientTools)
        {
            if (clientTools != null)
            {
                List<KernelFunction> kernelFunctions = new List<KernelFunction>();
                foreach (var tool in clientTools)
                {
#pragma warning disable SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                    // 创建原始的 KernelFunction
                    var originalFunction = tool.AsKernelFunction();
                    
                    // 创建带日志追踪的包装函数
                    var wrappedFunction = KernelFunctionFactory.CreateFromMethod(
                        async (KernelArguments arguments) =>
                        {
                            
                            try
                            {
                                // 调用原始函数
                                var result = await originalFunction.InvokeAsync(_kernel, arguments);
                                
                                
                                return result.GetValue<object>();
                            }
                            catch (Exception ex)
                            {
                                throw;
                            }
                        },
                        tool.Name,
                        tool.Description
                    );
                    
                    kernelFunctions.Add(wrappedFunction);
#pragma warning restore SKEXP0001 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
                }
                // 将所有函数作为一个插件导入
                _kernel.ImportPluginFromFunctions("MCPFunctions", kernelFunctions);
                
            }
        }

        /// <summary>
        /// 根据app配置的插件，导入插件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="_kernel"></param>
        public void ImportFunctions(LargeModelConfig ModelConfig, Kernel _kernel)
        {
            //插件不能重复注册，否则会异常
            if (_kernel.Plugins.Any(p => p.Name == "Functions"))
            {
                return;
            }
            List<KernelFunction> functions = new List<KernelFunction>();

            //API插件
            //ImportApiFunction(ModelInfo, functions);
            //本地函数插件
            ImportNativeFunction(ModelConfig, functions);

            _kernel.ImportPluginFromFunctions("Functions", functions);
        }
        public void ImportFunctions(Kernel _kernel, object type,string pluginName)
        {
            _kernel.Plugins.AddFromObject(type, pluginName);
        }
        

        /// <summary>
        /// 导入原生插件
        /// </summary>
        /// <param name="app"></param>
        /// <param name="functions"></param>
        private void ImportNativeFunction(LargeModelConfig ModelConfig, List<KernelFunction> functions)
        {
            if (ModelConfig.NativeFunction!=null)//需要添加判断应用是否开启了本地函数插件
            {
                //var nativeIdList = ModelConfig.NativeFunctionID.Split(",");

                _functionService.SearchMarkedMethods();
                using var scope = _serviceProvider.CreateScope();
                string pattern = "[^a-zA-Z0-9_]";

                foreach (var func in _functionService.Functions)
                {
                    if (ModelConfig.NativeFunction.Find(f => func.Key.Contains(Regex.Replace(f.Namespace + "_" + f.ClassName, pattern, "_"))) != null)
                    {
                        var methodInfo = _functionService.MethodInfos[func.Key];
                        var parameters = methodInfo.Parameters.Select(x => new KernelParameterMetadata(x.ParameterName) { ParameterType = x.ParameterType, Description = x.Description });
                        var returnType = new KernelReturnParameterMetadata() { ParameterType = methodInfo.ReturnType.ParameterType, Description = methodInfo.ReturnType.Description };
                        var target = ActivatorUtilities.CreateInstance(scope.ServiceProvider, func.Value.DeclaringType);
                        functions.Add(_kernel.CreateFunctionFromMethod(func.Value, target, func.Key, methodInfo.Description, parameters, returnType));
                    }
                }
            }
        }

        /// <summary>
        /// 注册默认插件
        /// </summary>
        /// <param name="kernel"></param>
        private void RegisterPluginsWithBase(IKernelBuilder kernel)
        {
#pragma warning disable SKEXP0050 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            //kernel.Plugins.AddFromObject(new BasePlugin(), "BasePlugin");
            kernel.Plugins.AddFromObject(new ConversationSummaryPlugin(), "ConversationSummaryPlugin");
            //kernel.Plugins.AddFromType<Microsoft.SemanticKernel.Plugins.Core.TimePlugin>();
            //kernel.Plugins.AddFromType<Microsoft.SemanticKernel.Plugins.Core.HttpPlugin>();
            //kernel.Plugins.AddFromType<Microsoft.SemanticKernel.Plugins.Core.MathPlugin>();
            //kernel.Plugins.AddFromType<Microsoft.SemanticKernel.Plugins.Core.FileIOPlugin>();
#pragma warning restore SKEXP0050 // 类型仅用于评估，在将来的更新中可能会被更改或删除。取消此诊断以继续。
            //kernel.Plugins.AddFromPromptDirectory(System.IO.Path.Combine(RepoFiles.SamplePluginsPath(), "SemanticFunction"));
        }

        /// <summary>
        /// 会话总结
        /// </summary>
        /// <param name="_kernel"></param>
        /// <param name="history"></param>
        /// <returns></returns>
        public async Task<string> HistorySummarize(Kernel _kernel, ChatHistory history)
        {
            KernelFunction sunFun = _kernel.Plugins.GetFunction("ConversationSummaryPlugin", "SummarizeConversation");
            var summary = await _kernel.InvokeAsync(sunFun, new() { ["input"] = $"内容是：{string.Join("\n", history.Select(x => x.Role + ": " + x.Content))} {Environment.NewLine}请注意，找出讨论的要点和得出的任何结论，不要加入其他常识，摘要是纯文本形式，用中文总结，直接输出总结内容，不需要增加而外格式(如：'总结：'这样的格式字眼)。" });
            string his = summary.GetValue<string>();
            return his;
        }

        public async Task<string> PromptFunctionCall(Kernel _kernel, CallFunction callFunction, KernelArguments parameter)
        {
            KernelFunction sunFun = _kernel.Plugins.GetFunction(callFunction.FunctionClassName, callFunction.FunctionName);

            var call = await _kernel.InvokeAsync(sunFun, parameter);
            return call.GetValue<string>();
        }

        /// <summary>
        /// 生成文本向量嵌入
        /// 使用 Microsoft.Extensions.AI 的 IEmbeddingGenerator 接口
        /// </summary>
        /// <param name="modelInfo">向量模型信息</param>
        /// <param name="text">要生成向量的文本</param>
        /// <returns>向量数组</returns>
        public async Task<float[]> GenerateEmbeddingAsync(LargeModelInfo modelInfo, string text)
        {
            try
            {
                if (modelInfo == null)
                {
                    throw new ArgumentNullException(nameof(modelInfo), "模型信息不能为空");
                }

                if (string.IsNullOrWhiteSpace(text))
                {
                    throw new ArgumentException("文本不能为空", nameof(text));
                }

                // 创建 HTTP 客户端
                var httpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(modelInfo.EndPoint);

                // 根据模型组织类型创建相应的嵌入生成服务
                IEmbeddingGenerator<string, Embedding<float>> embeddingService = modelInfo.ModelOrganizationID switch
                {
                    AIType.OpenAI => CreateOpenAIEmbeddingService(modelInfo, httpClient),
                    AIType.Bigmodel => CreateOpenAIEmbeddingService(modelInfo, httpClient),
                    AIType.DeepSeek => CreateOpenAIEmbeddingService(modelInfo, httpClient),
                    AIType.Ollama => CreateOllamaEmbeddingService(modelInfo, httpClient),
                    _ => CreateOpenAIEmbeddingService(modelInfo, httpClient) // 默认使用 OpenAI 兼容接口
                };

                // 生成向量嵌入
                var embedding = await embeddingService.GenerateAsync(text);

                if (embedding == null || embedding.Vector.Length == 0)
                {
                    throw new Exception("生成向量嵌入失败：返回结果为空");
                }

                var vector = embedding.Vector.ToArray();

                return vector;
            }
            catch (Exception ex)
            {
                throw new Exception($"生成向量嵌入失败: {ex.Message}", ex);
            }
        }

        /// <summary>
        /// 创建 OpenAI 兼容的嵌入生成服务
        /// </summary>
        private IEmbeddingGenerator<string, Embedding<float>> CreateOpenAIEmbeddingService(LargeModelInfo modelInfo, HttpClient httpClient)
        {
            var options = new OpenAIClientOptions
            {
                Endpoint = string.IsNullOrEmpty(modelInfo.EndPoint) ? null : new Uri(modelInfo.EndPoint)
            };
            ApiKeyCredential apiKeyCredential = new ApiKeyCredential(modelInfo.ModelKey);
            var client = new OpenAIClient(apiKeyCredential, options);
            var embeddingClient = client.GetEmbeddingClient(modelInfo.ModelName);
            return embeddingClient.AsIEmbeddingGenerator();
        }

        /// <summary>
        /// 创建 Ollama 嵌入生成服务
        /// </summary>
        private IEmbeddingGenerator<string, Embedding<float>> CreateOllamaEmbeddingService(LargeModelInfo modelInfo, HttpClient httpClient)
        {
            // Ollama 使用 OpenAI 兼容的接口
            return CreateOpenAIEmbeddingService(modelInfo, httpClient);
        }
    }
}
