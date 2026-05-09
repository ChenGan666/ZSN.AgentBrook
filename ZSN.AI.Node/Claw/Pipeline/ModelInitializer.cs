using Microsoft.Extensions.Logging;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Utils;
using ZSN.AI.Node.Claw.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.BLL;

namespace ZSN.AI.Node.Claw.Pipeline
{
    /// <summary>
    /// 模型初始化器 - 负责并行初始化Claw AI的所有模型配置
    /// </summary>
    public class ModelInitializer
    {
        private readonly ILogger _logger;

        // 模型配置缓存: Key = modelId, Value = (model, timestamp)
        private static readonly ConcurrentDictionary<int, (LargeModelInfo model, DateTime timestamp)> _modelCache
            = new ConcurrentDictionary<int, (LargeModelInfo, DateTime)>();

        // 缓存过期时间
        private static readonly TimeSpan _cacheTTL = TimeSpan.FromMinutes(5);

        public ModelInitializer(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 初始化所有模型配置 (并行获取)
        /// 策略: 先获取主模型(其他模型依赖它做fallback), 再并行获取其余6个
        /// </summary>
        public ModelInitializationResult InitializeAllModels(ClawAIData nodeData)
        {
            var result = new ModelInitializationResult
            {
                Logs = new List<string>()
            };

            result.Logs.Add("开始并行初始化模型配置...");

            // 阶段1: 先获取主模型 (其他模型的 ModelSelector 都依赖 nodeData.model 做 fallback)
            result.MainModelInfo = GetModelWithCache(nodeData.model.LargeModelID, "主模型");
            nodeData.model = result.MainModelInfo;
            result.MainModelConfig = new LargeModelConfig { Model = result.MainModelInfo };
            result.Logs.Add($"✓ 主模型: {result.MainModelInfo.ModelName}");

            // 阶段2: 并行获取其余6个模型
            var planningTask = Task.Run(() => GetModelWithCache(
                ModelSelector.GetPlanningModel(nodeData).LargeModelID, "规划模型"));
            var reflectionTask = Task.Run(() => GetModelWithCache(
                ModelSelector.GetReflectionModel(nodeData).LargeModelID, "反思模型"));
            var memoryTask = Task.Run(() => GetModelWithCache(
                ModelSelector.GetMemoryModel(nodeData).LargeModelID, "记忆模型"));
            var profileTask = Task.Run(() => GetModelWithCache(
                ModelSelector.GetProfileModel(nodeData).LargeModelID, "画像模型"));
            var personalityTask = Task.Run(() => GetModelWithCache(
                ModelSelector.GetPersonalityModel(nodeData).LargeModelID, "个性模型"));
            var embeddingTask = Task.Run(() => GetModelWithCache(
                ModelSelector.GetEmbeddingModel(nodeData).LargeModelID, "向量模型"));

            Task.WaitAll(planningTask, reflectionTask, memoryTask,
                         profileTask, personalityTask, embeddingTask);

            // 收集结果
            result.PlanningModelInfo = planningTask.Result;
            result.PlanningModelConfig = new LargeModelConfig { Model = result.PlanningModelInfo };

            result.ReflectionModelInfo = reflectionTask.Result;
            result.ReflectionModelConfig = new LargeModelConfig { Model = result.ReflectionModelInfo };

            result.MemoryModelInfo = memoryTask.Result;
            result.MemoryModelConfig = new LargeModelConfig { Model = result.MemoryModelInfo };

            result.ProfileModelInfo = profileTask.Result;
            result.ProfileModelConfig = new LargeModelConfig { Model = result.ProfileModelInfo };

            result.PersonalityModelInfo = personalityTask.Result;
            result.PersonalityModelConfig = new LargeModelConfig { Model = result.PersonalityModelInfo };

            result.EmbeddingModelInfo = embeddingTask.Result;
            result.EmbeddingModelConfig = new LargeModelConfig { Model = result.EmbeddingModelInfo };

            // 生成日志
            result.Logs.Add($"✓ 规划模型: {result.PlanningModelInfo.ModelName}" +
                (nodeData.taskPlanningConfig.useDedicatedModel && nodeData.planningModel != null ? " (专用)" : ""));
            result.Logs.Add($"✓ 反思模型: {result.ReflectionModelInfo.ModelName}" +
                (nodeData.reflectionConfig.useDedicatedModel && nodeData.reflectionModel != null ? " (专用)" : ""));
            result.Logs.Add($"✓ 记忆模型: {result.MemoryModelInfo.ModelName}" +
                (nodeData.memoryConfig.useDedicatedModel && nodeData.memoryModel != null ? " (专用)" : ""));
            result.Logs.Add($"✓ 画像模型: {result.ProfileModelInfo.ModelName}" +
                (nodeData.userProfileConfig.useDedicatedModel && nodeData.profileModel != null ? " (专用)" : ""));
            result.Logs.Add($"✓ 个性模型: {result.PersonalityModelInfo.ModelName}" +
                (nodeData.personalityConfig.useDedicatedModel && nodeData.personalityModel != null ? " (专用)" : ""));
            result.Logs.Add($"✓ 向量模型: {result.EmbeddingModelInfo.ModelName}" +
                (nodeData.embeddingModel != null ? " (专用)" : " (使用主模型)"));

            result.Logs.Add("✓ 模型配置并行初始化完成");

            return result;
        }

        /// <summary>
        /// 带缓存的模型获取
        /// </summary>
        private LargeModelInfo GetModelWithCache(int modelId, string modelType = "模型")
        {
            // 检查缓存
            if (_modelCache.TryGetValue(modelId, out var cached))
            {
                if (DateTime.Now - cached.timestamp < _cacheTTL)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MODEL_INIT,
                        $"{modelType}缓存命中 - ModelID: {modelId}");
                    return cached.model;
                }
                _modelCache.TryRemove(modelId, out _);
            }

            // 缓存未命中，从数据库获取
            var model = GetModelWithRetry(modelId, modelType);

            // 写入缓存
            _modelCache[modelId] = (model, DateTime.Now);

            return model;
        }

        /// <summary>
        /// 带重试机制的模型获取
        /// </summary>
        private LargeModelInfo GetModelWithRetry(int modelId, string modelType = "模型")
        {
            return RetryPolicy.Execute(
                () => {
                    var model = LargeModelInfoBussiness.GetModel(modelId);
                    if (model == null)
                        throw new Exception($"{modelType}不存在: ModelID={modelId}");
                    return model;
                },
                maxRetries: 3,
                delayMs: 500,
                onRetry: (ex, attempt) => {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.MODEL_INIT, $"获取{modelType}失败，第 {attempt} 次重试 - ModelID: {modelId}", ex);
                }
            );
        }

        /// <summary>
        /// 清除模型缓存
        /// </summary>
        public static void ClearCache()
        {
            _modelCache.Clear();
        }
    }

    /// <summary>
    /// 模型初始化结果
    /// </summary>
    public class ModelInitializationResult
    {
        public LargeModelInfo MainModelInfo { get; set; }
        public LargeModelConfig MainModelConfig { get; set; }

        public LargeModelInfo PlanningModelInfo { get; set; }
        public LargeModelConfig PlanningModelConfig { get; set; }

        public LargeModelInfo ReflectionModelInfo { get; set; }
        public LargeModelConfig ReflectionModelConfig { get; set; }

        public LargeModelInfo MemoryModelInfo { get; set; }
        public LargeModelConfig MemoryModelConfig { get; set; }

        public LargeModelInfo ProfileModelInfo { get; set; }
        public LargeModelConfig ProfileModelConfig { get; set; }

        public LargeModelInfo PersonalityModelInfo { get; set; }
        public LargeModelConfig PersonalityModelConfig { get; set; }

        public LargeModelInfo EmbeddingModelInfo { get; set; }
        public LargeModelConfig EmbeddingModelConfig { get; set; }

        public List<string> Logs { get; set; }
    }
}
