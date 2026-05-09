using System.Collections.Generic;
using System.Threading.Tasks;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// 记忆服务接口
    /// </summary>
    public interface IMemoryService
    {
        /// <summary>
        /// 构建记忆上下文
        /// </summary>
        Task<MemoryContext> BuildMemoryContextAsync(
            string AppID,
            string SessionID,
            string MemberID,
            List<Inputs> inputs,
            MemoryConfig config,
            string ClawID = null);

        /// <summary>
        /// 更新记忆
        /// </summary>
        Task UpdateMemoriesAsync(
            MemoryContext memoryContext,
            string originalTask,
            string finalResult,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string MemberID,
            string ClawID = null,
            LargeModelConfig embeddingModelConfig = null);

        /// <summary>
        /// 加载用户画像
        /// </summary>
        Task<UserProfile> LoadUserProfileAsync(string MemberID, string AppID, UserProfileConfig config);

        /// <summary>
        /// 更新用户画像
        /// </summary>
        Task UpdateUserProfileAsync(UserProfile userProfile);

        /// <summary>
        /// 加载 AI 个性状态
        /// </summary>
        Task<AIPersonalityState> LoadAIPersonalityStateAsync(string SessionID, PersonalityConfig config);

        /// <summary>
        /// 更新 AI 个性状态
        /// </summary>
        Task UpdateAIPersonalityStateAsync(AIPersonalityState aiState);

        /// <summary>
        /// 存储情景记忆
        /// </summary>
        Task StoreEpisodicMemoryAsync(EpisodicMemory memory);

        /// <summary>
        /// 检索相关记忆
        /// </summary>
        Task<List<EpisodicMemory>> RetrieveRelevantMemoriesAsync(
            string AppID,
            string MemberID,
            string query,
            int limit);
    }
}
