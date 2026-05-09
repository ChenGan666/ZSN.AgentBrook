using System.Threading.Tasks;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// AI 个性服务接口
    /// 负责管理 AI 的个性特征、情绪状态和个性化响应生成
    /// </summary>
    public interface IPersonalityService
    {
        /// <summary>
        /// 初始化 AI 个性状态
        /// 如果数据库中不存在,则根据配置创建默认个性
        /// </summary>
        /// <param name="SessionID">会话ID</param>
        /// <param name="AppID">应用ID</param>
        /// <param name="config">个性配置</param>
        /// <returns>AI个性状态</returns>
        Task<AIPersonalityState> InitializePersonalityAsync(
            string SessionID, 
            string AppID, 
            PersonalityConfig config);

        /// <summary>
        /// 应用个性化到提示词
        /// 根据个性特征和情绪状态调整系统提示词
        /// </summary>
        /// <param name="originalPrompt">原始提示词</param>
        /// <param name="aiState">AI个性状态</param>
        /// <param name="config">个性配置</param>
        /// <returns>个性化后的提示词</returns>
        Task<string> ApplyPersonalityToPromptAsync(
            string originalPrompt, 
            AIPersonalityState aiState, 
            PersonalityConfig config);

        /// <summary>
        /// 更新情绪状态
        /// 根据交互结果动态调整 AI 的情绪状态
        /// </summary>
        /// <param name="aiState">AI个性状态</param>
        /// <param name="interactionSuccess">交互是否成功</param>
        /// <param name="userFeedback">用户反馈(可选)</param>
        /// <param name="config">个性配置</param>
        /// <returns>更新后的情绪状态</returns>
        Task<AIPersonalityState> UpdateEmotionalStateAsync(
            AIPersonalityState aiState, 
            bool interactionSuccess, 
            string userFeedback = null,
            PersonalityConfig config = null);

        /// <summary>
        /// 更新目标状态
        /// 根据任务执行结果更新 AI 的当前目标
        /// </summary>
        /// <param name="aiState">AI个性状态</param>
        /// <param name="taskCompleted">任务是否完成</param>
        /// <param name="taskDescription">任务描述</param>
        /// <param name="config">个性配置</param>
        /// <returns>更新后的AI状态</returns>
        Task<AIPersonalityState> UpdateGoalsAsync(
            AIPersonalityState aiState, 
            bool taskCompleted, 
            string taskDescription,
            PersonalityConfig config = null);

        /// <summary>
        /// 生成个性化响应前缀
        /// 根据情绪状态生成响应的开场白或语气调整
        /// </summary>
        /// <param name="aiState">AI个性状态</param>
        /// <param name="config">个性配置</param>
        /// <returns>个性化前缀文本</returns>
        Task<string> GeneratePersonalizedPrefixAsync(
            AIPersonalityState aiState, 
            PersonalityConfig config);

        /// <summary>
        /// 分析交互质量并更新成功率
        /// </summary>
        /// <param name="SessionID">会话ID</param>
        /// <param name="taskSuccess">任务是否成功</param>
        /// <param name="qualityScore">质量评分(0-100)</param>
        /// <returns>更新后的成功率</returns>
        Task<decimal> UpdateSuccessRateAsync(
            string SessionID, 
            bool taskSuccess, 
            int qualityScore = 0);

        /// <summary>
        /// 获取个性化的系统消息
        /// 根据个性配置生成完整的系统消息
        /// </summary>
        /// <param name="baseSystemMessage">基础系统消息</param>
        /// <param name="aiState">AI个性状态</param>
        /// <param name="config">个性配置</param>
        /// <returns>个性化的系统消息</returns>
        Task<string> GetPersonalizedSystemMessageAsync(
            string baseSystemMessage,
            AIPersonalityState aiState,
            PersonalityConfig config);
    }
}
