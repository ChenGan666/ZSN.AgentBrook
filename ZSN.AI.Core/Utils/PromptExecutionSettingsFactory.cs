using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Connectors.OpenAI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AI.Core.Utils
{
    /// <summary>
    /// 多模型统一 Chat 执行参数
    /// OpenAI 参数 + ExtensionData 兜底
    /// </summary>
    public class UnifiedChatSettings : OpenAIPromptExecutionSettings
    {
        public UnifiedChatSettings()
        {
            // 确保 ExtensionData 可用
            ExtensionData ??= new Dictionary<string, object>();
        }

        /// <summary>
        /// 统一设置模型参数（最终都会进入请求体）
        /// </summary>
        public UnifiedChatSettings Set(string key, object value)
        {
            ExtensionData[key] = value;
            return this;
        }

        /// <summary>
        /// 安全读取扩展参数
        /// </summary>
        public T? Get<T>(string key)
        {
            if (ExtensionData != null &&
                ExtensionData.TryGetValue(key, out var value) &&
                value is T t)
            {
                return t;
            }
            return default;
        }
    }

    public static class PromptExecutionSettingsFactory
    {
        public static UnifiedChatSettings Create(LargeModelConfig modelConfig)
        {
            var settings = new UnifiedChatSettings
            {
                Temperature = modelConfig.Temperature / 100.0,
                TopP = modelConfig.TopPCoefficient / 100.0,
                FrequencyPenalty = 0,
                PresencePenalty = 0
            };

            // ===== Thinking =====
            ApplyThinking(
                settings,
                modelConfig.Model.ModelOrganizationID,
                modelConfig.Thinking
            );

            // ===== ResponseFormat =====
            if (!string.IsNullOrEmpty(modelConfig.ResponseFormat))
            {
                settings.ResponseFormat = modelConfig.ResponseFormat;
            }

            return settings;
        }

        private static void ApplyThinking(
            UnifiedChatSettings settings,
            AIType provider,
            bool thinking)
        {
            switch (provider)
            {
                case AIType.OpenAI:
                    // OpenAI 官方参数
                    settings.ReasoningEffort = thinking ? "high" : "low";
                    break;

                case AIType.QWen:
                    // Qwen3 私有参数
                    settings.Set("enable_thinking", thinking);
                    break;

                case AIType.DeepSeek:
                    settings.Set("thinking", thinking);
                    break;
            }
        }
    }


}
