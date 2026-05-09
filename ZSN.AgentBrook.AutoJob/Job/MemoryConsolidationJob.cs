using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Quartz;
using System;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.AutoJob
{
    /// <summary>
    /// ClawAI记忆整理定时任务
    /// 按配置的Cron表达式或固定间隔执行记忆整理
    /// 支持三级记忆层级的整理：会话级→ClawAI级→APP级
    /// </summary>
    [DisallowConcurrentExecution]
    public class MemoryConsolidationJob : JobBase, IJob
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<MemoryConsolidationJob> _logger;
        private readonly IConfiguration _configuration;
        private readonly IOptions<ClawAIOptions> _clawAIOptions;

        public MemoryConsolidationJob(
            IServiceProvider serviceProvider,
            ILogger<MemoryConsolidationJob> logger,
            IConfiguration configuration,
            IOptions<ClawAIOptions> clawAIOptions)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _configuration = configuration;
            _clawAIOptions = clawAIOptions;
        }

        Task IJob.Execute(IJobExecutionContext context)
        {
            return ExecuteConsolidationAsync(context);
        }

        private async Task ExecuteConsolidationAsync(IJobExecutionContext context)
        {
            var options = _clawAIOptions.Value?.MemoryConsolidation;
            if (options == null || !options.Enabled)
            {
                NLogHelper.WriteInfo("记忆整理服务未启用，跳过执行");
                return;
            }

            NLogHelper.WriteInfo("========== 记忆整理任务开始 ==========");

            try
            {
                // 0. 获取LLM模型
                var modelInfo = GetConsolidationModel();
                if (modelInfo == null)
                {
                    NLogHelper.WriteError("无法获取记忆整理LLM模型，终止执行");
                    return;
                }

                NLogHelper.WriteInfo($"使用LLM模型: {modelInfo.ModelName} (ID={modelInfo.LargeModelID})");

                // 0. 获取嵌入模型
                var embeddingModelInfo = GetEmbeddingModel();

                // 0. 计算增量时间窗口
                var cutoffTime = CalculateCutoffTime(context, options);
                NLogHelper.WriteInfo($"增量时间窗口: {cutoffTime:yyyy-MM-dd HH:mm:ss} 至今");

                // 1. 获取所有 AppID 列表
                var apps = AppInfoBussiness.GetList(" SystemStatus=2 ");
                if (apps == null || apps.Count == 0)
                {
                    NLogHelper.WriteInfo("未找到运行中的应用，跳过");
                    return;
                }

                if (options.MaxAppsPerRun > 0)
                {
                    apps = apps.Take(options.MaxAppsPerRun).ToList();
                }

                NLogHelper.WriteInfo($"找到 {apps.Count} 个运行中的应用");

                // 创建 Scope 获取 Scoped 服务
                using var scope = _serviceProvider.CreateScope();
                var consolidationService = scope.ServiceProvider
                    .GetRequiredService<IMemoryConsolidationService>();

                // 2. 遍历每个 AppID
                foreach (var app in apps)
                {
                    try
                    {
                        NLogHelper.WriteInfo($"--- 处理应用: {app.AppID} ({app.Name}) ---");

                        // 2a. 获取该 AppID 下所有 ClawAI 节点（从长期记忆中提取去重的ClawID）
                        var clawIds = GetClawIdsForApp(app.AppID);
                        NLogHelper.WriteInfo($"应用 {app.AppID} 下有 {clawIds.Count} 个 ClawAI 节点");

                        // 2b. 对每个 ClawID 执行 ClawAI 级整理
                        foreach (var clawId in clawIds)
                        {
                            try
                            {
                                var clawResult = await consolidationService.ConsolidateClawAIAsync(
                                    app.AppID, clawId, cutoffTime, modelInfo, embeddingModelInfo);
                                NLogHelper.WriteInfo($"ClawAI {clawId} 整理完成: {clawResult.Summary}");
                            }
                            catch (Exception ex)
                            {
                                NLogHelper.WriteError($"ClawAI {clawId} 整理失败: {ex.Message}");
                            }
                        }

                        // 2c. 执行 APP 级整理
                        try
                        {
                            var appResult = await consolidationService.ConsolidateAppAsync(
                                app.AppID, cutoffTime, modelInfo, embeddingModelInfo);
                            NLogHelper.WriteInfo($"APP {app.AppID} 级整理完成: {appResult.Summary}");
                        }
                        catch (Exception ex)
                        {
                            NLogHelper.WriteError($"APP {app.AppID} 级整理失败: {ex.Message}");
                        }
                    }
                    catch (Exception ex)
                    {
                        NLogHelper.WriteError($"处理应用 {app.AppID} 失败: {ex.Message}");
                    }
                }

                NLogHelper.WriteInfo("========== 记忆整理任务完成 ==========");
            }
            catch (Exception ex)
            {
                NLogHelper.WriteError($"记忆整理任务异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 获取记忆整理使用的LLM模型
        /// 优先从 LargeModel.MemoryConsolidationModelID 配置获取，失败回退到默认模型
        /// </summary>
        private LargeModelInfo GetConsolidationModel()
        {
            try
            {
                var modelIdStr = _configuration["LargeModel:MemoryConsolidationModelID"];
                if (!string.IsNullOrEmpty(modelIdStr) && int.TryParse(modelIdStr, out int modelId) && modelId > 0)
                {
                    var model = LargeModelInfoBussiness.GetModel(modelId);
                    if (model != null)
                    {
                        NLogHelper.WriteInfo($"从配置获取整理模型: ID={modelId}");
                        return model;
                    }
                }

                // 回退到默认模型
                var defaultModel = LargeModelInfoBussiness.GetDefaultModel();
                NLogHelper.WriteInfo($"使用默认整理模型: {defaultModel?.ModelName}");
                return defaultModel;
            }
            catch (Exception ex)
            {
                NLogHelper.WriteError($"获取整理模型失败: {ex.Message}");
                return LargeModelInfoBussiness.GetDefaultModel();
            }
        }

        /// <summary>
        /// 获取嵌入模型
        /// 从 LargeModel.EmbeddingModelID 配置获取
        /// </summary>
        private LargeModelInfo GetEmbeddingModel()
        {
            try
            {
                var modelIdStr = _configuration["LargeModel:EmbeddingModelID"];
                if (!string.IsNullOrEmpty(modelIdStr) && int.TryParse(modelIdStr, out int modelId) && modelId > 0)
                {
                    var model = LargeModelInfoBussiness.GetModel(modelId);
                    if (model != null)
                    {
                        NLogHelper.WriteInfo($"使用嵌入模型: {model.ModelName} (ID={modelId})");
                        return model;
                    }
                }

                NLogHelper.WriteInfo("未配置嵌入模型(EmbeddingModelID)，提升的知识将不生成向量");
                return null;
            }
            catch (Exception ex)
            {
                NLogHelper.WriteError($"获取嵌入模型失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 计算增量时间窗口截止时间
        /// Cron定时模式：使用 CronLookbackMinutes（默认24小时）
        /// 周期模式：使用 间隔秒数/60 * IntervalLookbackMultiplier（默认1.5倍）
        /// </summary>
        private DateTime CalculateCutoffTime(IJobExecutionContext context, MemoryConsolidationOptions options)
        {
            // 从 Job 配置获取 LoopTimerSeconds
            var loopTimerSeconds = 0;
            var jobConfig = context?.JobDetail?.JobDataMap;
            if (jobConfig != null && jobConfig.ContainsKey("LoopTimerSeconds"))
            {
                int.TryParse(jobConfig.GetString("LoopTimerSeconds"), out loopTimerSeconds);
            }

            double lookbackMinutes;

            if (loopTimerSeconds <= 0)
            {
                // Cron定时模式：使用固定回查时间
                lookbackMinutes = options.CronLookbackMinutes;
                NLogHelper.WriteInfo($"Cron定时模式，回查 {lookbackMinutes} 分钟");
            }
            else
            {
                // 周期模式：间隔 * 倍数
                lookbackMinutes = loopTimerSeconds / 60.0 * options.IntervalLookbackMultiplier;
                NLogHelper.WriteInfo($"周期模式（间隔{loopTimerSeconds}秒），回查 {lookbackMinutes:F1} 分钟");
            }

            return DateTime.Now.AddMinutes(-lookbackMinutes);
        }

        /// <summary>
        /// 获取指定AppID下所有去重的ClawID列表
        /// 从长期记忆表中提取去重的ClawID
        /// </summary>
        private System.Collections.Generic.List<string> GetClawIdsForApp(string appId)
        {
            var clawIds = new System.Collections.Generic.List<string>();

            try
            {
                var memories = LongTermMemoryBusiness.GetByApp(appId, 5000);
                clawIds = memories
                    .Where(m => !string.IsNullOrEmpty(m.ClawID))
                    .Select(m => m.ClawID)
                    .Distinct()
                    .ToList();
            }
            catch (Exception ex)
            {
                NLogHelper.WriteError($"获取ClawID列表失败: {ex.Message}");
            }

            return clawIds;
        }
    }
}
