using System.Threading.Tasks;
using ZSN.AI.Node.Claw.Models;

namespace ZSN.AI.Node.Claw.Interfaces
{
    /// <summary>
    /// 智能主控服务接口
    /// 使用LLM理解上下文和系统提示词，智能判断是否需要任务规划
    /// </summary>
    public interface IMasterControlService
    {
        /// <summary>
        /// 判断用户输入应该直接回复还是进行任务规划
        /// </summary>
        /// <param name="context">主控判断上下文</param>
        /// <returns>主控判断结果</returns>
        Task<MasterControlResult> DecideAsync(MasterControlContext context);

        /// <summary>
        /// 清除缓存
        /// </summary>
        void ClearCache();

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        /// <returns>缓存命中率等统计信息</returns>
        string GetCacheStats();
    }
}
