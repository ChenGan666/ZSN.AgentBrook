using ZSN.AI.Entity;
using ZSN.AI.Node.ServiceDesk.Models;

namespace ZSN.AI.Node.ServiceDesk.Interfaces
{
    /// <summary>会话状态管理器接口</summary>
    public interface ISessionStateManager
    {
        /// <summary>
        /// 获取或创建会话状态
        /// </summary>
        Task<SessionStateContext> GetOrCreateSessionStateAsync(
            string sessionId,
            string appId,
            string memberId);

        /// <summary>
        /// 转换会话状态
        /// </summary>
        Task<SessionStateContext> TransitionStateAsync(
            SessionStateContext currentState,
            SessionState targetState,
            string reason = null);

        /// <summary>
        /// 收集用户信息
        /// </summary>
        Task<SessionStateContext> CollectInformationAsync(
            SessionStateContext state,
            string userMessage,
            IntentRule intentRule);

        /// <summary>
        /// 生成缺失字段的提示消息
        /// </summary>
        string GeneratePromptForMissingFields(SessionStateContext state);

        /// <summary>
        /// 保存会话状态
        /// </summary>
        Task SaveSessionStateAsync(SessionStateContext state);
    }
}
