using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// 模型选择器 - 根据配置选择合适的模型
    /// </summary>
    public static class ModelSelector
    {
        /// <summary>
        /// 获取任务规划模型
        /// </summary>
        public static LargeModelInfo GetPlanningModel(ClawAIData nodeData)
        {
            if (nodeData.taskPlanningConfig.useDedicatedModel && nodeData.planningModel != null)
            {
                return nodeData.planningModel;
            }
            return nodeData.model; // 回退到主模型
        }

        /// <summary>
        /// 获取反思评估模型
        /// </summary>
        public static LargeModelInfo GetReflectionModel(ClawAIData nodeData)
        {
            if (nodeData.reflectionConfig.useDedicatedModel && nodeData.reflectionModel != null)
            {
                return nodeData.reflectionModel;
            }
            return nodeData.model; // 回退到主模型
        }

        /// <summary>
        /// 获取记忆处理模型
        /// </summary>
        public static LargeModelInfo GetMemoryModel(ClawAIData nodeData)
        {
            if (nodeData.memoryConfig.useDedicatedModel && nodeData.memoryModel != null)
            {
                return nodeData.memoryModel;
            }
            return nodeData.model; // 回退到主模型
        }

        /// <summary>
        /// 获取用户画像模型
        /// </summary>
        public static LargeModelInfo GetProfileModel(ClawAIData nodeData)
        {
            if (nodeData.userProfileConfig.useDedicatedModel && nodeData.profileModel != null)
            {
                return nodeData.profileModel;
            }
            // 优先使用记忆模型,再回退到主模型
            if (nodeData.memoryModel != null)
            {
                return nodeData.memoryModel;
            }
            return nodeData.model;
        }

        /// <summary>
        /// 获取 AI 个性模型
        /// </summary>
        public static LargeModelInfo GetPersonalityModel(ClawAIData nodeData)
        {
            if (nodeData.personalityConfig.useDedicatedModel && nodeData.personalityModel != null)
            {
                return nodeData.personalityModel;
            }
            return nodeData.model; // 回退到主模型
        }

        /// <summary>
        /// 获取向量模型（用于生成文本向量嵌入）
        /// </summary>
        public static LargeModelInfo GetEmbeddingModel(ClawAIData nodeData)
        {
            if (nodeData.embeddingModel != null)
            {
                return nodeData.embeddingModel;
            }
            // 回退到主模型（主模型也可以支持 embedding）
            return nodeData.model;
        }

        /// <summary>
        /// 获取主控判断模型（使用主模型）
        /// </summary>
        public static LargeModelInfo GetMasterControlModel(ClawAIData nodeData)
        {
            return nodeData.model;
        }

        /// <summary>
        /// 获取主 AI 模型
        /// </summary>
        public static LargeModelInfo GetMainModel(ClawAIData nodeData)
        {
            return nodeData.model;
        }
    }
}
