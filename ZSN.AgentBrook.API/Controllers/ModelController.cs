using Microsoft.AspNetCore.Mvc;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.Service.Controllers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using ChatHistory = Microsoft.SemanticKernel.ChatCompletion.ChatHistory;
using ErrorCode = ZSN.AI.Entity.ErrorCode;

namespace ZSN.AgentBrook.API.Controllers
{
    /// <summary>
    /// Agent 模式服务端"暴露层"。仅 2 个新增接口 + 只读复用：
    ///  N1 GetList    —— 暴露脱敏的大模型列表，供客户端选择"编排大脑"模型；
    ///  N2 Completion —— 裸 LLM 代理（SSE），客户端用它驱动 Plan-Act-Reflect。
    /// 设计原则：密钥仅在服务端使用、不下发；预设系统提示词按 MConfig 配置注入、不回显；
    ///           完全不触碰 Chat / App 控制器与业务表（Chat 零侵入）。
    /// </summary>
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Member")]
    [Route("api/[controller]/[action]")]
    public class ModelController : ApiBaseController
    {
        private readonly IChatService _chatService;
        private readonly IKernelService _kernelService;

        public ModelController(IChatService chatService, IKernelService kernelService)
        {
            _chatService = chatService;
            _kernelService = kernelService;
        }

        /// <summary>
        /// N1：获取可用的"编排大脑"模型列表（脱敏）。
        /// 入参：PostData（经 ApiBaseController 解密为 JsonObj）。
        /// 出参：AgentModelDTO 列表，绝不包含 ModelKey/EndPoint/MConfig 等敏感字段。
        /// 数据源：SystemStatus=Normal(0) 且 TypeCode=Chat(1) 的模型。
        /// </summary>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<List<AgentModelDTO>> GetList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") == -1)
            {
                return JsonMsg<List<AgentModelDTO>>.Error(null, ErrorCode.DataFormatError);
            }

            List<LargeModelInfo> models = LargeModelInfoBussiness.GetList(" SystemStatus=0 AND TypeCode=1 ");

            string previewHost = ConfigHelper.GetString("previewHost");

            List<AgentModelDTO> result = new List<AgentModelDTO>(models.Count);
            foreach (var m in models)
            {
                result.Add(new AgentModelDTO
                {
                    LargeModelID = m.LargeModelID,
                    Name = m.Name,
                    ModelName = m.ModelName,
                    MICON = string.IsNullOrEmpty(m.MICON) ? string.Empty : string.Format(previewHost, m.MICON),
                    TypeCode = m.TypeCode,
                    Thinking = m.Thinking,
                    Description = m.Description
                });
            }

            return JsonMsg<List<AgentModelDTO>>.OK(result);
        }

        /// <summary>
        /// N2：裸 LLM 代理（Agent 编排大脑）。
        /// 入参（JsonObj）：{ modelID:int, messages:GptMsg[], stream:bool, temperature?:double(0~100), responseFormat?:string }
        /// 响应：
        ///   stream=true  —— SSE，逐 token 推 {"delta":"…"}，结束推 {"done":true}；
        ///   stream=false —— 一次性返回完整文本 JsonMsg。
        /// 安全：modelID 必须落在已启用、Chat 类型的模型集合内（越权/不存在 → 返回错误码，不抛敏感信息）；
        ///       预设系统提示词按该模型 MConfig 的 mergeStrategy 注入，**不下发、不回显**。
        /// </summary>
        [ApiExplorerSettings(GroupName = "V1-Member")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public async Task<IActionResult> Completion([FromBody] PostData paramValue, CancellationToken cancellationToken)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") == -1)
            {
                return BadRequest(JsonMsg<object>.Error(null, ErrorCode.DataFormatError));
            }

            int modelID = jObject.JsonGetValue<int>("modelID", 0);
            List<GptMsg> messages = jObject.JsonGetValue<List<GptMsg>>("messages") ?? new List<GptMsg>();
            bool stream = jObject.JsonGetValue<bool>("stream", true);
            double temperature = jObject.JsonGetValue<double?>("temperature") ?? 70d;
            double topP = jObject.JsonGetValue<double?>("topP") ?? 90d;
            string responseFormat = jObject.JsonGetValue<string>("responseFormat", "text");

            // 1) 取模型 + 越权 / 可用范围校验。
            LargeModelInfo model = modelID > 0 ? LargeModelInfoBussiness.GetModel(modelID) : null;
            if (model == null || model.SystemStatus != LargeModelStatus.Normal || (int)model.TypeCode != (int)AIModelType.Chat)
                return Ok(JsonMsg<object>.Error(null, ErrorCode.NoModel));

            // 2) 解析预设系统提示词并按 mergeStrategy 组装 ChatHistory。
            ChatHistory history = BuildHistoryWithPreset(model, messages);

            // 3) 构造 LargeModelConfig。
            LargeModelConfig config = new LargeModelConfig
            {
                Model = model,
                Temperature = temperature,
                TopPCoefficient = topP,
                ResponseFormat = string.IsNullOrWhiteSpace(responseFormat) ? "text" : responseFormat
            };

            // 4) 执行 + 响应。
            if (stream)
            {
                Response.ContentType = "text/event-stream";
                Response.Headers.Append("Cache-Control", "no-cache");
                Response.Headers.Append("Connection", "keep-alive");
                await Response.StartAsync(cancellationToken);

                try
                {
                    var kernel = _kernelService.GetKernel(model);
                    var chat = kernel.GetRequiredService<IChatCompletionService>();
                    var settings = PromptExecutionSettingsFactory.Create(config);

                    await foreach (var chunk in chat.GetStreamingChatMessageContentsAsync(history, settings, kernel, cancellationToken))
                    {
                        if (chunk?.Content != null && chunk.Content.Length > 0)
                        {
                            var delta = chunk.Content?.ToString() ?? "";
                            if (!string.IsNullOrEmpty(delta))
                            {
                                var sseLine = $"data: {JsonConvert.SerializeObject(new { delta })}\n\n";
                                await Response.WriteAsync(sseLine, Encoding.UTF8, cancellationToken);
                                await Response.Body.FlushAsync(cancellationToken);
                            }
                        }
                    }

                    var doneLine = $"data: {JsonConvert.SerializeObject(new { done = true })}\n\n";
                    await Response.WriteAsync(doneLine, Encoding.UTF8, cancellationToken);
                    await Response.Body.FlushAsync(cancellationToken);

                    return new EmptyResult();
                }
                catch (OperationCanceledException)
                {
                    return new EmptyResult();
                }
            }
            else
            {
                var sb = new StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    config, history,
                    Function: null,
                    responseFormat: config.ResponseFormat,
                    enableStreamingObservation: false,
                    ct: cancellationToken))
                {
                    sb.Append(chunk);
                }

                return Ok(JsonMsg<object>.OK(new { content = sb.ToString() }));
            }
        }

        /// <summary>
        /// 解析模型 MConfig 中的预设系统提示词，并按 mergeStrategy 与调用方 messages 组装 ChatHistory。
        ///   prepend(默认)：preset 作为首条 system，其后接调用方 messages；
        ///   override：仅用 preset 作为 system，丢弃调用方传入的 system（保留 user/assistant）；
        ///   append：调用方 system 在前，preset 追加其后。
        /// 空或非 JSON 的 MConfig 视为"无预设"，记 warn 后继续（不报错）。
        /// </summary>
        private static ChatHistory BuildHistoryWithPreset(LargeModelInfo model, List<GptMsg> messages)
        {
            var history = new ChatHistory();

            ModelMConfig mConfig = ModelMConfig.Parse(model.MConfig);
            string preset = mConfig.EffectivePreset;
            string strategy = (mConfig.MergeStrategy ?? "prepend").Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(preset))
            {
                if (strategy == "override")
                {
                    history.AddSystemMessage(preset);
                    foreach (var msg in messages)
                    {
                        if (!string.Equals(msg.role, "system", StringComparison.OrdinalIgnoreCase))
                        {
                            AppendMessage(history, msg);
                        }
                    }
                    return history;
                }

                if (strategy == "append")
                {
                    foreach (var msg in messages)
                    {
                        AppendMessage(history, msg);
                    }
                    history.AddSystemMessage(preset);
                    return history;
                }

                // 默认 prepend：preset 作为首条 system。
                history.AddSystemMessage(preset);
            }

            foreach (var msg in messages)
            {
                AppendMessage(history, msg);
            }
            return history;
        }

        /// <summary>
        /// 将单条 GptMsg 按 role 映射到 ChatHistory（system/user/assistant）。
        /// </summary>
        private static void AppendMessage(ChatHistory history, GptMsg msg)
        {
            if (msg == null || string.IsNullOrEmpty(msg.content))
            {
                return;
            }
            string role = (msg.role ?? string.Empty).Trim().ToLowerInvariant();
            switch (role)
            {
                case "system":
                    history.AddSystemMessage(msg.content);
                    break;
                case "assistant":
                    history.AddAssistantMessage(msg.content);
                    break;
                case "user":
                default:
                    history.AddUserMessage(msg.content);
                    break;
            }
        }
    }
}
