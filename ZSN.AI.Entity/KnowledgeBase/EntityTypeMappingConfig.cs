using System;
using System.Collections.Generic;

namespace ZSN.AI.Entity.KnowledgeBase
{
    /// <summary>
    /// 实体类型映射配置
    /// </summary>
    public class EntityTypeMappingConfig
    {
        /// <summary>
        /// 配置版本
        /// </summary>
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 最后更新时间
        /// </summary>
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 是否启用自动学习
        /// </summary>
        public bool AutoLearnEnabled { get; set; } = true;

        /// <summary>
        /// 自动添加映射的最小频率阈值
        /// </summary>
        public int MinFrequencyForAutoAdd { get; set; } = 10;

        /// <summary>
        /// 静态映射（手动维护的标准映射）
        /// </summary>
        public Dictionary<string, string> StaticMappings { get; set; } = new();

        /// <summary>
        /// 学习到的映射（自动添加的映射）
        /// </summary>
        public Dictionary<string, string> LearnedMappings { get; set; } = new();

        /// <summary>
        /// 类型统计信息
        /// </summary>
        public Dictionary<string, TypeStatistics> Statistics { get; set; } = new();
    }

    /// <summary>
    /// 类型统计信息
    /// </summary>
    public class TypeStatistics
    {
        /// <summary>
        /// 出现频率
        /// </summary>
        public int Frequency { get; set; }

        /// <summary>
        /// 首次出现时间
        /// </summary>
        public DateTime FirstSeen { get; set; }

        /// <summary>
        /// 最后出现时间
        /// </summary>
        public DateTime LastSeen { get; set; }

        /// <summary>
        /// 自动添加时间（如果已自动添加）
        /// </summary>
        public DateTime? AutoAddedAt { get; set; }

        /// <summary>
        /// 建议的映射类型
        /// </summary>
        public string? SuggestedMapping { get; set; }
    }
}
