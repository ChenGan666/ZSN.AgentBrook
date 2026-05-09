using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Models
{
    /// <summary>
    /// 后处理参数 (已废弃,使用 PostProcessingSnapshot 代替)
    /// </summary>
    [Obsolete("使用 PostProcessingSnapshot 代替,避免共享可变状态")]
    public class PostProcessingParams
    {
        public MemoryContext MemoryContext { get; set; }
        public string OriginalTask { get; set; }
        public string FinalResult { get; set; }
        public TaskPlanning TaskPlanning { get; set; }
        public string AppID { get; set; }
        public string SessionID { get; set; }
        public string MemberID { get; set; }
        public string ClawID { get; set; }
        public PersonalityConfig PersonalityConfig { get; set; }
        public string StreamKey { get; set; }
        public string ProcessesID { get; set; }
        public string TaskID { get; set; }
        public string NodeID { get; set; }
        public string RecordID { get; set; }
        public List<Output> Outputs { get; set; }
        public List<string> Logs { get; set; }
        public ReflectionResult ReflectionResult { get; set; }

        /// <summary>
        /// P2优化: 后处理专用日志列表 (独立于主线程Logs，避免并发问题)
        /// </summary>
        public List<string> PostLogs { get; } = new List<string>();

        /// <summary>
        /// 向量模型配置（用于生成 embedding）
        /// </summary>
        public LargeModelConfig EmbeddingModelConfig { get; set; }
    }

    /// <summary>
    /// 后处理快照 (不可变,线程安全)
    /// 用于后台队列,避免共享可变状态
    /// </summary>
    public class PostProcessingSnapshot
    {
        /// <summary>
        /// 记忆上下文 (深拷贝)
        /// </summary>
        public MemoryContext MemoryContext { get; init; }

        /// <summary>
        /// 原始任务
        /// </summary>
        public string OriginalTask { get; init; }

        /// <summary>
        /// 最终结果
        /// </summary>
        public string FinalResult { get; init; }

        /// <summary>
        /// 任务规划 (序列化后的 JSON)
        /// </summary>
        public TaskPlanning TaskPlanning { get; init; }

        /// <summary>
        /// 应用ID
        /// </summary>
        public string AppID { get; init; }

        /// <summary>
        /// 会话ID
        /// </summary>
        public string SessionID { get; init; }

        /// <summary>
        /// 成员ID
        /// </summary>
        public string MemberID { get; init; }

        /// <summary>
        /// Claw节点ID
        /// </summary>
        public string ClawID { get; init; }

        /// <summary>
        /// 个性配置 (深拷贝)
        /// </summary>
        public PersonalityConfig PersonalityConfig { get; init; }

        /// <summary>
        /// 流式推送Key
        /// </summary>
        public string StreamKey { get; init; }

        /// <summary>
        /// 流程ID
        /// </summary>
        public string ProcessesID { get; init; }

        /// <summary>
        /// 任务ID
        /// </summary>
        public string TaskID { get; init; }

        /// <summary>
        /// 节点ID
        /// </summary>
        public string NodeID { get; init; }

        /// <summary>
        /// 向量模型配置
        /// </summary>
        public LargeModelConfig EmbeddingModelConfig { get; init; }

        /// <summary>
        /// 从可变参数创建不可变快照
        /// </summary>
        public static PostProcessingSnapshot CreateFrom(PostProcessingParams param)
        {
            return new PostProcessingSnapshot
            {
                // 深拷贝复杂对象,避免引用共享
                MemoryContext = DeepCopy(param.MemoryContext),
                OriginalTask = param.OriginalTask,
                FinalResult = param.FinalResult,
                TaskPlanning = DeepCopy(param.TaskPlanning),
                AppID = param.AppID,
                SessionID = param.SessionID,
                MemberID = param.MemberID,
                ClawID = param.ClawID,
                PersonalityConfig = DeepCopy(param.PersonalityConfig),
                StreamKey = param.StreamKey,
                ProcessesID = param.ProcessesID,
                TaskID = param.TaskID,
                NodeID = param.NodeID,
                EmbeddingModelConfig = param.EmbeddingModelConfig
            };
        }

        /// <summary>
        /// 深拷贝对象 (使用 JSON 序列化/反序列化)
        /// </summary>
        private static T DeepCopy<T>(T obj)
        {
            if (obj == null) return default(T);
            var json = JsonConvert.SerializeObject(obj);
            return JsonConvert.DeserializeObject<T>(json);
        }
    }
}
