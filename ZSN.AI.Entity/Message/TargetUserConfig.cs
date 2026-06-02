using System;
using System.Collections.Generic;

namespace ZSN.AI.Entity
{
    /// <summary>
    /// 目标用户配置（MessageNode 使用）
    /// </summary>
    public class TargetUserConfig
    {
        /// <summary>
        /// 用户来源模式: Static / Dynamic / Query
        /// </summary>
        public string SourceMode { get; set; } = "Static";

        /// <summary>
        /// 模式对应的数据源
        /// Static: 忽略，使用 UserList
        /// Dynamic: 上游节点变量名（JSON数组格式）
        /// Query: 查询条件表达式（预留）
        /// </summary>
        public string SourceValue { get; set; } = string.Empty;

        /// <summary>
        /// Static 模式下直接填写的用户列表
        /// </summary>
        public List<TargetUserItem> UserList { get; set; } = new List<TargetUserItem>();

        /// <summary>
        /// 是否批量单发: true=每人一条独立消息, false=作为群聊发送
        /// </summary>
        public bool SendIndividually { get; set; } = true;
    }
}
