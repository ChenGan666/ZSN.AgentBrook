using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.ClawAI
{
    /// <summary>
    /// ClawAI 异步恢复回调持有者
    /// BLL 层通过此静态委托触发恢复，Node 层在启动时注册具体实现
    /// 避免 BLL → Node 的循环依赖
    /// </summary>
    public static class ClawAIResumeCallback
    {
        /// <summary>
        /// 恢复回调：传入 asyncTaskID + 合并后的步骤结果
        /// </summary>
        private static Func<string, string, Dictionary<string, string>, Task<string>> _resumeAsync;

        /// <summary>
        /// 注册恢复回调（由 Node 层在启动时调用）
        /// </summary>
        public static void Register(Func<string, string, Dictionary<string, string>, Task<string>> resumeAsync)
        {
            _resumeAsync = resumeAsync;
        }

        /// <summary>
        /// 触发恢复（由 BLL 层调用）
        /// </summary>
        public static async Task<string> ResumeAsync(string asyncTaskID, string mergedResult, Dictionary<string, string> allStepResults)
        {
            if (_resumeAsync == null)
            {
                throw new InvalidOperationException("ClawAI 恢复回调未注册。请在应用启动时调用 ClawAIResumeCallback.Register()");
            }
            return await _resumeAsync(asyncTaskID, mergedResult, allStepResults);
        }

        /// <summary>
        /// 检查回调是否已注册
        /// </summary>
        public static bool IsRegistered => _resumeAsync != null;
    }
}
