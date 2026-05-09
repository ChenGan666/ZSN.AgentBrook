using Microsoft.AspNetCore.Mvc.RazorPages;
using NPOI.SS.Formula.Functions;
using NPOI.SS.UserModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AI.Entity
{
    public partial class LargeModelConfig
    {
        public LargeModelConfig() { }
        public LargeModelInfo Model { get; set; } = new LargeModelInfo();
        public string Id { get; set; } = string.Empty;

        public string? Prompt { get; set; }
        /// <summary>
        /// 温度
        /// </summary>
        public double Temperature { get; set; } = 70f;
        /// <summary>
        /// 多样性
        /// </summary>
        public double TopPCoefficient { get; set; } = 90f;
        /// <summary>
        /// 相似度
        /// </summary>
        public double Relevance { get; set; } = 70f;

        /// <summary>
        /// 向量匹配数
        /// </summary>
        public int MaxMatchesCount { get; set; } = 3;
        public int RerankCount { get; set; } = 20;
        /// <summary>
        /// 提问最大token数
        /// </summary>
        public int MaxAskPromptSize { get; set; } = 2048;
        /// <summary>
        /// 回答最大token数
        /// </summary>
        public int AnswerTokens { get; set; } = 2048;

        /// <summary>
        /// 是否开启思考链
        /// </summary>
        public bool Thinking { get;set; } = false;
        /// <summary>
        /// 输出类型:json_object，text
        /// </summary>
        public string ResponseFormat { get; set; } = "text";

        public List<PluginsInfo> SemanticFunction { get; set; } = new List<PluginsInfo>();
        //public string? SemanticFunctionName { get; set; } = string.Empty;
        public List<PluginsInfo> NativeFunction { get; set; } = new List<PluginsInfo>();
        //public string? NativeFunctionName { get; set; } = string.Empty;

        public McpInfo Mcp { get; set; } = new McpInfo();
    }

    public partial class KnowledgeBaseUnit { 
        public KnowledgeBaseUnit() { }
        public LargeModelUnit LargeModelUnit { get; set; } = new LargeModelUnit();
        public KnowledgeBaseInfo KnowledgeBaseInfo { get; set; }
    }

    public partial class LargeModelUnit
    {
        public LargeModelUnit() { }
        public LargeModelConfig ChatModel { get; set; } = new LargeModelConfig();
        public LargeModelConfig RerankModel { get; set; } = new LargeModelConfig();
        public LargeModelConfig VectorModel { get; set; } = new LargeModelConfig();

        public LargeModelUnit ModelMap(AIModelType ModelType,  LargeModelInfo LargeModel)
        {
            var modelMap = new Dictionary<AIModelType, Action<LargeModelUnit, LargeModelInfo>>()
                {
                    { AIModelType.Chat, (unit, model) => unit.ChatModel.Model = model },
                    { AIModelType.Embedding, (unit, model) => unit.VectorModel.Model = model },
                    { AIModelType.Rerank, (unit, model) => unit.RerankModel.Model = model }
                };

            if (modelMap.TryGetValue(ModelType, out var assignModel))
            {
                assignModel(this, LargeModel);
            }

            return this;
        }
    }

}
