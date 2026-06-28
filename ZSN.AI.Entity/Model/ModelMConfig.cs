using System;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// tb_large_model_info.MConfig 的结构化解析模型。
    /// MConfig 列以 JSON 字符串存储，用于承载模型的预设配置（含 Agent 模式编排大脑的系统提示词）。
    /// 兼容空值 / 非 JSON 旧数据：解析失败时视为"无预设"，不报错。
    /// </summary>
    public partial class ModelMConfig
    {
        /// <summary>
        /// 合并策略：
        /// prepend(默认) = 预设作为首条 system，其后接调用方传入的 messages；
        /// override      = 仅用预设作为 system，丢弃调用方传入的 system 消息（保留 user/assistant）；
        /// append        = 调用方 system 在前，预设追加其后。
        /// </summary>
        [JsonPropertyName("mergeStrategy")]
        public string MergeStrategy { get; set; } = "prepend";

        /// <summary>该模型的预设系统提示词（Agent 模式必注入）。</summary>
        [JsonPropertyName("systemPrompt")]
        public string? SystemPrompt { get; set; }

        /// <summary>可选：Agent 模式专用提示词，存在时优先于 SystemPrompt。</summary>
        [JsonPropertyName("agentSystemPrompt")]
        public string? AgentSystemPrompt { get; set; }

        /// <summary>格式版本（可选，默认 1）。</summary>
        [JsonPropertyName("version")]
        public int Version { get; set; } = 1;

        /// <summary>
        /// 取生效预设提示词：优先 AgentSystemPrompt，其次 SystemPrompt。
        /// </summary>
        [JsonIgnore]
        public string? EffectivePreset => !string.IsNullOrWhiteSpace(AgentSystemPrompt)
            ? AgentSystemPrompt
            : SystemPrompt;

        /// <summary>
        /// 容错解析 MConfig 字符串。空 / 非 JSON → 返回全默认实例（视为无预设），由调用方决定是否记 warn。
        /// </summary>
        public static ModelMConfig Parse(string mConfig)
        {
            if (string.IsNullOrWhiteSpace(mConfig))
            {
                return new ModelMConfig();
            }

            try
            {
                var cfg = JsonSerializer.Deserialize<ModelMConfig>(mConfig, JsonOptions);
                return cfg ?? new ModelMConfig();
            }
            catch
            {
                // 存量数据可能为非 JSON 的纯文本，按"无预设"处理（不报错）。
                return new ModelMConfig();
            }
        }

        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
        };
    }
}
