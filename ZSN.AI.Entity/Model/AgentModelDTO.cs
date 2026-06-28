using System;
using System.Text.Json.Serialization;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// Agent 模式编排大脑模型列表的脱敏出参 DTO（N1 / ModelController.GetList）。
    /// 仅暴露安全字段，严禁返回 ModelKey / EndPoint / MConfig / ApiKey 等任何敏感模型配置。
    /// </summary>
    public partial class AgentModelDTO
    {
        /// <summary>模型主键（LargeModelID）。</summary>
        public int LargeModelID { get; set; }

        /// <summary>模型显示名称。</summary>
        public string Name { get; set; } = string.Empty;

        /// <summary>上游模型标识（如 gpt-4o）。</summary>
        public string ModelName { get; set; } = string.Empty;

        /// <summary>模型图标（已拼接 previewHost，可直接展示）。</summary>
        public string MICON { get; set; } = string.Empty;

        /// <summary>模型类型（Chat=1 / Embedding / Rerank）。</summary>
        public AIModelType TypeCode { get; set; }

        /// <summary>是否为思考链模型。</summary>
        public bool Thinking { get; set; }

        /// <summary>模型描述。</summary>
        public string Description { get; set; } = string.Empty;
    }
}
