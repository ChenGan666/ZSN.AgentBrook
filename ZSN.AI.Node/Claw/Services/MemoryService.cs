using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.BLL;
using System.Text;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 记忆服务实现
    /// </summary>
    public class MemoryService : IMemoryService
    {
        private readonly ILogger<MemoryService> _logger;
        private readonly ClawAIOptions _options;
        private readonly IKnowledgeExtractionService _knowledgeExtractionService;
        private readonly IKernelService _kernelService;

        public MemoryService(
            ILogger<MemoryService> logger,
            IOptions<ClawAIOptions> options,
            IKnowledgeExtractionService knowledgeExtractionService,
            IKernelService kernelService)
        {
            _logger = logger;
            _options = options?.Value ?? new ClawAIOptions();
            _knowledgeExtractionService = knowledgeExtractionService;
            _kernelService = kernelService;
        }

        public async Task<MemoryContext> BuildMemoryContextAsync(
            string AppID,
            string SessionID,
            string MemberID,
            List<Inputs> inputs,
            MemoryConfig config,
            string ClawID = null)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 构建记忆上下文 - MemberID: {MemberID}");

            var context = new MemoryContext
            {
                WorkingMemoryCount = 0,
                RelevantMemories = new List<EpisodicMemory>()
            };

            try
            {
                // 1. 加载用户画像
                if (config.enableWorkingMemory)
                {
                    context.UserProfile = await LoadUserProfileAsync(MemberID, AppID, new UserProfileConfig { enabled = true });
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 用户画像已加载");
                }

                // 2. 加载 AI 状态
                context.AIState = await LoadAIPersonalityStateAsync(SessionID, new PersonalityConfig { enabled = true });
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " AI 状态已加载");

                // 3. 加载短期工作记忆 (ChatHistory)
                if (config.enableWorkingMemory)
                {
                    var workingMemory = await LoadWorkingMemoryAsync(AppID, SessionID, _options.Memory.WorkingMemoryLimit);
                    context.WorkingMemoryCount = workingMemory.Count;
                    context.WorkingMemory = workingMemory;  // 保存实际内容
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 短期记忆已加载: {context.WorkingMemoryCount} 条");
                }

                // 4. 加载情景记忆
                if (config.enableEpisodicMemory)
                {
                    var episodicMemories = await LoadEpisodicMemoriesAsync(AppID, SessionID, MemberID, _options.Memory.EpisodicMemoryLimit);
                    if (episodicMemories != null && episodicMemories.Count > 0)
                    {
                        context.RelevantMemories.AddRange(episodicMemories);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 情景记忆已加载: {episodicMemories.Count} 条");
                    }
                }

                // 5. 从知识库检索相关内容 (如果有查询)
                if (config.enableLongTermMemory && inputs != null)
                {
                    var query = inputs.FirstOrDefault(i => i.varname == "prompt")?.value;
                    if (!string.IsNullOrEmpty(query))
                    {
                        var knowledgeResults = await SearchKnowledgeBaseAsync(AppID, ClawID, query, _options.Memory.LongTermMemoryLimit);
                        if (knowledgeResults != null && knowledgeResults.Count > 0)
                        {
                            // 将知识库结果转换为情景记忆格式
                            foreach (var kb in knowledgeResults)
                            {
                                context.RelevantMemories.Add(new EpisodicMemory
                                {
                                    Summary = kb,
                                    EventType = "knowledge_base",
                                    Importance = 70
                                });
                            }
                            LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 知识库检索结果: {knowledgeResults.Count} 条");
                        }
                    }
                }
                
                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[Memory] 记忆上下文构建完成 - 工作记忆: {context.WorkingMemoryCount}, 相关记忆: {context.RelevantMemories?.Count ?? 0}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 构建记忆上下文失败");
            }
            
            return context;
        }

        public async Task UpdateMemoriesAsync(
            MemoryContext memoryContext,
            string originalTask,
            string finalResult,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID,
            string MemberID,
            string ClawID = null,
            LargeModelConfig embeddingModelConfig = null)
        {
            LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 更新记忆 - MemberID: {MemberID}");

            try
            {
                // 1. 更新用户画像
                if (memoryContext.UserProfile != null)
                {
                    await UpdateUserProfileAsync(memoryContext.UserProfile, originalTask, finalResult, AppID, MemberID);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 用户画像已更新");
                }

                // 2. 更新 AI 个性状态
                if (memoryContext.AIState != null)
                {
                    await UpdateAIPersonalityStateAsync(memoryContext.AIState, taskPlanning, AppID, SessionID);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " AI 状态已更新");
                }

                // 3. 保存重要情景记忆
                await SaveTaskExecutionMemoryAsync(
                    AppID, SessionID, MemberID, originalTask, finalResult, taskPlanning);
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 任务执行记忆已保存");

                // 4. 压缩和归档长期记忆 (如果需要)
                await CompressAndArchiveMemoriesAsync(AppID, SessionID, MemberID, ClawID, originalTask, finalResult, taskPlanning, embeddingModelConfig);
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 记忆压缩和归档已完成");

                // 5. 自动知识提炼 (如果启用)
                if (_options.Memory.EnableAutoKnowledgeExtraction && _knowledgeExtractionService != null)
                {
                    await AutoExtractKnowledgeAsync(
                        AppID, ClawID, SessionID, MemberID,
                        originalTask, finalResult, memoryContext, embeddingModelConfig);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 自动知识提炼已完成");
                }

                // 优化5: 自动触发知识去重合并
                if (_knowledgeExtractionService != null && ShouldTriggerMerge(AppID, MemberID))
                {
                    // 在后台异步执行去重，不阻塞主流程
                    _ = Task.Run(async () => 
                    {
                        try
                        {
                            int mergedCount = await _knowledgeExtractionService.MergeAndDeduplicateKnowledgeAsync(
                                AppID, MemberID);
                            LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                                $"[AutoMerge] 知识去重完成 - 合并数量: {mergedCount}");
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "[AutoMerge] 知识去重失败");
                        }
                    });
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 记忆更新完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 更新记忆失败");
            }
        }

        public async Task<UserProfile> LoadUserProfileAsync(string MemberID, string AppID, UserProfileConfig config)
        {
            try
            {
                // 从数据库加载用户画像
                var entity = UserProfileBusiness.GetByMemberAndApp(MemberID, AppID);
                
                if (entity != null)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 从数据库加载用户画像 - ProfileID: {entity.ProfileID}");
                    
                    return new UserProfile
                    {
                        MemberID = MemberID,
                        AppID = AppID,
                        PreferencesSummary = entity.PreferencesSummary ?? "暂无偏好数据",
                        PreferencesDetail = entity.PreferencesDetail,
                        InteractionPatternSummary = entity.InteractionPatternsSummary ?? "暂无交互模式数据",
                        TotalInteractions = entity.TotalInteractions,
                        LastInteractionTime = entity.LastInteractionTime
                    };
                }
                else
                {
                    // 数据库中不存在,返回默认值
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 用户画像不存在,返回默认值 - MemberID: {MemberID}, AppID: {AppID}");
                    
                    return new UserProfile
                    {
                        MemberID = MemberID,
                        AppID = AppID,
                        PreferencesSummary = "暂无偏好数据",
                        InteractionPatternSummary = "暂无交互模式数据",
                        TotalInteractions = 0,
                        LastInteractionTime = DateTime.Now
                    };
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Memory] 加载用户画像失败 - MemberID: {MemberID}, AppID: {AppID}");
                return new UserProfile 
                { 
                    MemberID = MemberID,
                    AppID = AppID,
                    PreferencesSummary = "暂无偏好数据",
                    InteractionPatternSummary = "暂无交互模式数据"
                };
            }
        }

        public async Task UpdateUserProfileAsync(UserProfile userProfile)
        {
            try
            {
                if (string.IsNullOrEmpty(userProfile.AppID))
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.MEMORY,$" 用户画像缺少AppID,无法更新 - MemberID: {userProfile.MemberID}");
                    return;
                }
                
                // 更新用户画像到数据库(需要先查询是否存在)
                var entity = UserProfileBusiness.GetByMemberAndApp(userProfile.MemberID, userProfile.AppID);
                
                if (entity != null)
                {
                    // 更新现有记录
                    entity.PreferencesSummary = userProfile.PreferencesSummary;
                    entity.PreferencesDetail = userProfile.PreferencesDetail;
                    entity.InteractionPatternsSummary = userProfile.InteractionPatternSummary;
                    entity.TotalInteractions = userProfile.TotalInteractions;
                    entity.LastInteractionTime = userProfile.LastInteractionTime;
                    entity.LastUpdateTime = DateTime.Now;
                    
                    UserProfileBusiness.Update(entity);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 用户画像已更新 - ProfileID: {entity.ProfileID}");
                }
                else
                {
                    // 创建新记录
                    var newEntity = new UserProfileInfo
                    {
                        ProfileID = Guid.NewGuid().ToString(),
                        MemberID = userProfile.MemberID,
                        AppID = userProfile.AppID,
                        PreferencesSummary = userProfile.PreferencesSummary,
                        PreferencesDetail = userProfile.PreferencesDetail,
                        InteractionPatternsSummary = userProfile.InteractionPatternSummary,
                        TotalInteractions = userProfile.TotalInteractions,
                        LastInteractionTime = userProfile.LastInteractionTime,
                        CreateTime = DateTime.Now,
                        LastUpdateTime = DateTime.Now
                    };
                    
                    UserProfileBusiness.Add(newEntity);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 用户画像已创建 - ProfileID: {newEntity.ProfileID}");
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Memory] 更新用户画像失败 - MemberID: {userProfile.MemberID}");
            }
        }

        public async Task<AIPersonalityState> LoadAIPersonalityStateAsync(string SessionID, PersonalityConfig config)
        {
            try
            {
                // 从数据库加载AI个性状态
                var entity = AIPersonalityStateBusiness.GetBySessionID(SessionID);
                
                if (entity != null)
                {
                    return new AIPersonalityState
                    {
                        SessionID = SessionID,
                        PersonalityTraits = string.IsNullOrEmpty(entity.PersonalityTraits) 
                            ? new Dictionary<string, object>() 
                            : JsonConvert.DeserializeObject<Dictionary<string, object>>(entity.PersonalityTraits),
                        EmotionalState = string.IsNullOrEmpty(entity.EmotionalState)
                            ? new Dictionary<string, object>()
                            : JsonConvert.DeserializeObject<Dictionary<string, object>>(entity.EmotionalState),
                        CurrentGoals = string.IsNullOrEmpty(entity.CurrentGoals)
                            ? new List<string>()
                            : JsonConvert.DeserializeObject<List<string>>(entity.CurrentGoals)
                    };
                }
                
                return new AIPersonalityState { SessionID = SessionID };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Memory] 加载AI状态失败 - SessionID: {SessionID}");
                return new AIPersonalityState { SessionID = SessionID };
            }
        }

        public async Task UpdateAIPersonalityStateAsync(AIPersonalityState aiState)
        {
            try
            {
                // 更新AI个性状态到数据库
                var entity = AIPersonalityStateBusiness.GetBySessionID(aiState.SessionID);
                
                if (entity != null)
                {
                    entity.PersonalityTraits = JsonConvert.SerializeObject(aiState.PersonalityTraits ?? new Dictionary<string, object>());
                    entity.EmotionalState = JsonConvert.SerializeObject(aiState.EmotionalState ?? new Dictionary<string, object>());
                    entity.CurrentGoals = JsonConvert.SerializeObject(aiState.CurrentGoals ?? new List<string>());
                    entity.LastUpdateTime = DateTime.Now;
                    
                    AIPersonalityStateBusiness.Update(entity);
                    AIPersonalityStateBusiness.IncrementInteractions(entity.StateID);
                }
                
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 更新AI状态 - SessionID: {aiState.SessionID}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Memory] 更新AI状态失败 - SessionID: {aiState.SessionID}");
            }
        }

        public async Task StoreEpisodicMemoryAsync(EpisodicMemory memory)
        {
            try
            {
                // 保存情景记忆到数据库
                var entity = new EpisodicMemoryInfo
                {
                    MemoryID = Guid.NewGuid().ToString(),
                    AppID = memory.AppID,
                    SessionID = memory.SessionID,
                    MemberID = memory.MemberID,
                    EventType = memory.EventType,
                    EventContext = JsonConvert.SerializeObject(memory.EventContext ?? new Dictionary<string, object>()),
                    EventResult = memory.EventResult,
                    Summary = memory.Summary,
                    Embedding = memory.Embedding,
                    Importance = memory.Importance,
                    AccessCount = 0,
                    CreateTime = DateTime.Now
                };
                
                EpisodicMemoryBusiness.Add(entity);
                
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 保存情景记忆 - EventType: {memory.EventType}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Memory] 保存情景记忆失败 - EventType: {memory.EventType}");
            }
        }

        public async Task<List<EpisodicMemory>> RetrieveRelevantMemoriesAsync(
            string AppID,
            string MemberID,
            string query,
            int limit)
        {
            try
            {
                // 从数据库检索相关记忆(按重要性排序)
                var entities = EpisodicMemoryBusiness.GetByMemberAndApp(MemberID, AppID, limit);
                
                var memories = entities.Select(e => new EpisodicMemory
                {
                    AppID = e.AppID,
                    SessionID = e.SessionID,
                    MemberID = e.MemberID,
                    EventType = e.EventType,
                    EventContext = string.IsNullOrEmpty(e.EventContext)
                        ? new Dictionary<string, object>()
                        : JsonConvert.DeserializeObject<Dictionary<string, object>>(e.EventContext),
                    EventResult = e.EventResult,
                    Summary = e.Summary,
                    Embedding = e.Embedding,
                    Importance = e.Importance
                }).ToList();

                // 批量增加访问次数 - 优化：使用并行处理提升性能
                if (entities.Count > 0)
                {
                    var memoryIds = entities.Select(e => e.MemoryID).ToList();
                    // 使用并行处理加速访问计数更新
                    System.Threading.Tasks.Parallel.ForEach(memoryIds, memoryId =>
                    {
                        try
                        {
                            EpisodicMemoryBusiness.IncrementAccessCount(memoryId);
                        }
                        catch (Exception ex)
                        {
                            // 单个更新失败不影响整体，只记录日志
                            _logger.LogWarning(ex, $"[Memory] 增加访问次数失败 - MemoryID: {memoryId}");
                        }
                    });
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 检索记忆 - MemberID: {MemberID}, Count: {memories.Count}");
                
                return memories;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"[Memory] 检索记忆失败 - MemberID: {MemberID}");
                return new List<EpisodicMemory>();
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 加载短期工作记忆 (ChatHistory)
        /// </summary>
        private async Task<List<AppChatLogInfo>> LoadWorkingMemoryAsync(string AppID, string SessionID, int limit)
        {
            try
            {
                // 获取最近的对话历史
                var chatLogs = AppChatLogInfoBussiness.GetListBySessionID(AppID, SessionID);
                if (chatLogs != null && chatLogs.Count > 0)
                {
                    // 取最近N条
                    return chatLogs.OrderByDescending(c => c.CreateTime)
                        .Take(limit)
                        .OrderBy(c => c.CreateTime)
                        .ToList();
                }
                
                return new List<AppChatLogInfo>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 加载工作记忆失败");
                return new List<AppChatLogInfo>();
            }
        }

        /// <summary>
        /// 加载情景记忆
        /// </summary>
        private async Task<List<EpisodicMemory>> LoadEpisodicMemoriesAsync(
            string AppID,
            string SessionID,
            string MemberID,
            int limit)
        {
            try
            {
                // 从数据库加载情景记忆(按重要性和时间排序)
                List<EpisodicMemoryInfo> entities = EpisodicMemoryBusiness.GetByMemberAndApp(MemberID, AppID, limit);
                
                if (entities != null && entities.Count > 0)
                {
                    var memories = entities.Select(e => new EpisodicMemory
                    {
                        MemoryID = e.MemoryID,
                        EventType = e.EventType,
                        EventContext = JsonConvert.DeserializeObject<Dictionary<string, object>>(e.EventContext ?? "{}"),
                        EventResult = e.EventResult,
                        Summary = e.Summary,
                        Importance = e.Importance,
                        CreateTime = e.CreateTime
                    }).ToList();
                    
                    return memories;
                }
                
                return new List<EpisodicMemory>();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 加载情景记忆失败");
                return new List<EpisodicMemory>();
            }
        }

        /// <summary>
        /// 从知识库检索相关内容(使用独立的长期记忆知识库)
        /// </summary>
        private async Task<List<string>> SearchKnowledgeBaseAsync(string AppID, string ClawID, string query, int limit)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 长期记忆知识库检索 - Query: {query}, Limit: {limit}");
                
                var results = new List<string>();
                
                // 方案1: 使用关键词匹配(简单快速,无需向量嵌入)
                // 如果指定ClawID,则只检索该ClawAI节点的记忆
                var memories = string.IsNullOrEmpty(ClawID) 
                    ? LongTermMemoryBusiness.SearchByKeywords(AppID, query, limit)
                    : LongTermMemoryBusiness.SearchByClawAndKeywords(AppID, ClawID, query, limit);
                
                if (memories != null && memories.Count > 0)
                {
                    // 批量增加访问次数 - 优化：使用并行处理提升性能
                    var memoryIds = memories.Select(m => m.MemoryID).ToList();
                    System.Threading.Tasks.Parallel.ForEach(memoryIds, memoryId =>
                    {
                        try
                        {
                            LongTermMemoryBusiness.IncrementAccessCount(memoryId);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, $"[Memory] 增加长期记忆访问次数失败 - MemoryID: {memoryId}");
                        }
                    });

                    // 格式化输出
                    foreach (var memory in memories)
                    {
                        var result = new StringBuilder();
                        result.AppendLine($"[知识类型: {memory.KnowledgeType}]");
                        if (!string.IsNullOrEmpty(memory.Topic))
                        {
                            result.AppendLine($"[主题: {memory.Topic}]");
                        }
                        result.AppendLine($"摘要: {memory.Summary}");
                        if (!string.IsNullOrEmpty(memory.Content) && memory.Content.Length <= 500)
                        {
                            result.AppendLine($"内容: {memory.Content}");
                        }
                        result.AppendLine($"[重要性: {memory.Importance}, 访问次数: {memory.AccessCount}]");
                        
                        results.Add(result.ToString());
                    }
                    
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 长期记忆检索成功 - 找到 {memories.Count} 条相关知识");
                }
                else
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 长期记忆检索 - 未找到相关知识");
                }
                
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 长期记忆知识库检索失败");
                return new List<string>();
            }
        }

        /// <summary>
        /// 更新用户画像
        /// </summary>
        private async Task UpdateUserProfileAsync(
            UserProfile profile,
            string originalTask,
            string finalResult,
            string AppID,
            string MemberID)
        {
            try
            {
                // 1. 分析用户偏好变化
                var preferences = AnalyzeUserPreferences(originalTask, finalResult, profile);
                
                // 2. 更新交互模式
                var interactionPattern = AnalyzeInteractionPattern(originalTask, profile);
                
                // 3. 增加交互次数
                profile.TotalInteractions++;
                profile.LastInteractionTime = DateTime.Now;
                
                // 4. 更新偏好摘要
                if (!string.IsNullOrEmpty(preferences))
                {
                    profile.PreferencesSummary = UpdateSummary(
                        profile.PreferencesSummary, preferences, maxLength: _options.Memory.UserProfileMaxLength);
                }

                // 5. 更新交互模式摘要
                if (!string.IsNullOrEmpty(interactionPattern))
                {
                    profile.InteractionPatternSummary = UpdateSummary(
                        profile.InteractionPatternSummary, interactionPattern, maxLength: _options.Memory.InteractionPatternMaxLength);
                }
                
                // 6. 保存到数据库
                try
                {
                    // 先尝试获取现有记录
                    var existingProfile = UserProfileBusiness.GetByMemberAndApp(MemberID, AppID);
                    
                    if (existingProfile != null)
                    {
                        // 更新现有记录
                        existingProfile.PreferencesSummary = profile.PreferencesSummary;
                        existingProfile.InteractionPatternsSummary = profile.InteractionPatternSummary;
                        existingProfile.TotalInteractions = profile.TotalInteractions;
                        existingProfile.LastInteractionTime = profile.LastInteractionTime;
                        existingProfile.LastUpdateTime = DateTime.Now;
                        
                        UserProfileBusiness.Update(existingProfile);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 用户画像已更新到数据库 - ProfileID: {existingProfile.ProfileID}");
                    }
                    else
                    {
                        // 创建新记录
                        var newProfile = new UserProfileInfo
                        {
                            ProfileID = Guid.NewGuid().ToString(),
                            MemberID = MemberID,
                            AppID = AppID,
                            PreferencesSummary = profile.PreferencesSummary,
                            InteractionPatternsSummary = profile.InteractionPatternSummary,
                            TotalInteractions = profile.TotalInteractions,
                            LastInteractionTime = profile.LastInteractionTime,
                            CreateTime = DateTime.Now,
                            LastUpdateTime = DateTime.Now
                        };
                        
                        UserProfileBusiness.Add(newProfile);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" 用户画像已创建到数据库 - ProfileID: {newProfile.ProfileID}");
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "[Memory] 保存用户画像到数据库失败");
                }
                
                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[Memory] 用户画像已更新 - MemberID: {MemberID}, 总交互次数: {profile.TotalInteractions}");
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 更新用户画像失败");
            }
        }

        /// <summary>
        /// 分析用户偏好
        /// </summary>
        private string AnalyzeUserPreferences(string task, string result, UserProfile profile)
        {
            var preferences = new List<string>();
            
            // 分析任务类型偏好
            if (task.Contains("分析") || task.Contains("数据"))
            {
                preferences.Add("数据分析");
            }
            if (task.Contains("生成") || task.Contains("创建"))
            {
                preferences.Add("内容生成");
            }
            if (task.Contains("总结") || task.Contains("摘要"))
            {
                preferences.Add("文本总结");
            }
            if (task.Contains("翻译"))
            {
                preferences.Add("语言翻译");
            }
            if (task.Contains("搜索") || task.Contains("查找"))
            {
                preferences.Add("信息检索");
            }
            
            // 分析任务长度偏好
            if (task.Length > 200)
            {
                preferences.Add("详细描述");
            }
            else if (task.Length < 50)
            {
                preferences.Add("简洁表达");
            }
            
            return preferences.Count > 0 
                ? $"偏好: {string.Join(", ", preferences)}" 
                : string.Empty;
        }

        /// <summary>
        /// 分析交互模式
        /// </summary>
        private string AnalyzeInteractionPattern(string task, UserProfile profile)
        {
            var patterns = new List<string>();
            
            // 分析提问方式
            if (task.Contains("?") || task.Contains("？"))
            {
                patterns.Add("提问式");
            }
            else if (task.StartsWith("请") || task.StartsWith("帮"))
            {
                patterns.Add("请求式");
            }
            else
            {
                patterns.Add("指令式");
            }
            
            // 分析交互时间模式
            var hour = DateTime.Now.Hour;
            if (hour >= 6 && hour < 12)
            {
                patterns.Add("上午活跃");
            }
            else if (hour >= 12 && hour < 18)
            {
                patterns.Add("下午活跃");
            }
            else if (hour >= 18 && hour < 24)
            {
                patterns.Add("晚上活跃");
            }
            else
            {
                patterns.Add("深夜活跃");
            }
            
            // 分析交互频率
            if (profile.TotalInteractions > 0)
            {
                var daysSinceFirst = (DateTime.Now - profile.LastInteractionTime).TotalDays;
                if (daysSinceFirst > 0)
                {
                    var frequency = profile.TotalInteractions / daysSinceFirst;
                    if (frequency > 10)
                    {
                        patterns.Add("高频用户");
                    }
                    else if (frequency > 3)
                    {
                        patterns.Add("中频用户");
                    }
                    else
                    {
                        patterns.Add("低频用户");
                    }
                }
            }
            
            return patterns.Count > 0 
                ? $"模式: {string.Join(", ", patterns)}" 
                : string.Empty;
        }

        /// <summary>
        /// 更新摘要信息
        /// </summary>
        private string UpdateSummary(string currentSummary, string newInfo, int maxLength)
        {
            if (string.IsNullOrEmpty(currentSummary) || currentSummary == "暂无偏好数据" || currentSummary == "暂无交互模式数据")
            {
                return newInfo;
            }
            
            // 合并新旧信息
            var combined = $"{currentSummary}; {newInfo}";
            
            // 如果超过最大长度,保留最新的信息
            if (combined.Length > maxLength)
            {
                var parts = combined.Split(';');
                var result = new List<string>();
                int currentLength = 0;
                
                // 从最新的开始保留
                for (int i = parts.Length - 1; i >= 0; i--)
                {
                    var part = parts[i].Trim();
                    if (currentLength + part.Length + 2 <= maxLength)
                    {
                        result.Insert(0, part);
                        currentLength += part.Length + 2;
                    }
                    else
                    {
                        break;
                    }
                }
                
                return string.Join("; ", result);
            }
            
            return combined;
        }

        /// <summary>
        /// 更新 AI 个性状态
        /// </summary>
        private async Task UpdateAIPersonalityStateAsync(
            AIPersonalityState state,
            TaskPlanning taskPlanning,
            string AppID,
            string SessionID)
        {
            try
            {
                // 从字典中获取或初始化交互次数和成功率
                int interactionCount = state.PersonalityTraits.ContainsKey("InteractionCount") 
                    ? Convert.ToInt32(state.PersonalityTraits["InteractionCount"]) 
                    : 0;
                
                double successRate = state.PersonalityTraits.ContainsKey("SuccessRate") 
                    ? Convert.ToDouble(state.PersonalityTraits["SuccessRate"]) 
                    : 0.0;
                
                // 更新交互次数
                interactionCount++;
                
                // 计算成功率
                if (taskPlanning.PlanningStatus == PlanningStatus.Completed)
                {
                    // 成功任务,提高成功率
                    successRate = (successRate * (interactionCount - 1) + 100) / interactionCount;
                }
                else if (taskPlanning.PlanningStatus == PlanningStatus.Failed)
                {
                    // 失败任务,降低成功率
                    successRate = (successRate * (interactionCount - 1)) / interactionCount;
                }
                
                // 保存回字典
                state.PersonalityTraits["InteractionCount"] = interactionCount;
                state.PersonalityTraits["SuccessRate"] = successRate;
                state.LastUpdateTime = DateTime.Now;
                
                
                // 保存到数据库
                try
                {
                    // 先尝试获取现有记录
                    var existingState = AIPersonalityStateBusiness.GetBySessionID(SessionID);
                    
                    if (existingState != null)
                    {
                        // 更新现有记录
                        existingState.PersonalityTraits = JsonConvert.SerializeObject(state.PersonalityTraits);
                        existingState.EmotionalState = JsonConvert.SerializeObject(state.EmotionalState);
                        existingState.CurrentGoals = JsonConvert.SerializeObject(state.CurrentGoals);
                        existingState.InteractionCount = interactionCount;
                        existingState.SuccessRate = (decimal)successRate;
                        existingState.LastUpdateTime = DateTime.Now;
                        
                        AIPersonalityStateBusiness.Update(existingState);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" AI状态已更新到数据库 - StateID: {existingState.StateID}");
                    }
                    else
                    {
                        // 创建新记录
                        var newState = new AIPersonalityStateInfo
                        {
                            StateID = Guid.NewGuid().ToString(),
                            SessionID = SessionID,
                            AppID = AppID,
                            PersonalityTraits = JsonConvert.SerializeObject(state.PersonalityTraits),
                            EmotionalState = JsonConvert.SerializeObject(state.EmotionalState),
                            CurrentGoals = JsonConvert.SerializeObject(state.CurrentGoals),
                            InteractionCount = interactionCount,
                            SuccessRate = (decimal)successRate,
                            CreateTime = DateTime.Now,
                            LastUpdateTime = DateTime.Now
                        };
                        
                        AIPersonalityStateBusiness.Add(newState);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, $" AI状态已创建到数据库 - StateID: {newState.StateID}");
                    }
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "[Memory] 保存AI状态到数据库失败");
                }
                
                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[Memory] AI 状态已更新 - 交互次数: {interactionCount}, 成功率: {successRate:F2}%");
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 更新 AI 状态失败");
            }
        }

        /// <summary>
        /// 保存任务执行记忆
        /// </summary>
        private async Task SaveTaskExecutionMemoryAsync(
            string AppID,
            string SessionID,
            string MemberID,
            string originalTask,
            string finalResult,
            TaskPlanning taskPlanning)
        {
            try
            {
                // 创建任务执行记忆
                var memory = new EpisodicMemory
                {
                    MemoryID = Guid.NewGuid().ToString(),
                    AppID = AppID,
                    SessionID = SessionID,
                    MemberID = MemberID,
                    EventType = "task_execution",
                    EventContext = new Dictionary<string, object>
                    {
                        { "task", originalTask },
                        { "planningID", taskPlanning.PlanningID },
                        { "totalSteps", taskPlanning.TotalSteps },
                        { "strategy", taskPlanning.Metadata.Strategy },
                        { "revisionCount", taskPlanning.Metadata.RevisionCount }
                    },
                    EventResult = finalResult,
                    Summary = $"任务: {originalTask.Substring(0, Math.Min(50, originalTask.Length))}... " +
                             $"状态: {taskPlanning.PlanningStatus}, 步骤: {taskPlanning.TotalSteps}",
                    Importance = CalculateMemoryImportance(taskPlanning),
                    CreateTime = DateTime.Now
                };
                
                // 保存到数据库
                await StoreEpisodicMemoryAsync(memory);
                
                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[Memory] 任务执行记忆已保存 - Importance: {memory.Importance}, Summary: {memory.Summary}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 保存任务执行记忆失败");
            }
        }

        /// <summary>
        /// 优化7: 计算记忆重要性（动态评分）
        /// </summary>
        private int CalculateMemoryImportance(TaskPlanning taskPlanning, bool isCompleted = true)
        {
            int importance = 50; // 基础重要性
            
            // 1. 根据步骤数量调整
            if (taskPlanning.TotalSteps >= 5)
            {
                importance += 10; // 复杂任务更重要
            }
            else if (taskPlanning.TotalSteps >= 3)
            {
                importance += 5;
            }
            
            // 2. 根据修订次数调整
            if (taskPlanning.Metadata.RevisionCount > 0)
            {
                importance += Math.Min(15, taskPlanning.Metadata.RevisionCount * 5); // 最多加15分
            }
            
            // 3. 根据执行状态调整
            if (isCompleted)
            {
                importance += 20; // 成功完成的任务更重要
            }
            else
            {
                importance += 15; // 失败的任务也重要(学习经验)
            }
            
            // 4. 根据执行时长调整（耗时长的任务可能更复杂）
            if (taskPlanning.Metadata.ActualDuration > 60)
            {
                importance += 5; // 耗时超过1分钟
            }
            
            // 5. 根据步骤质量评分调整
            if (taskPlanning.Steps != null && taskPlanning.Steps.Count > 0)
            {
                var avgQuality = taskPlanning.Steps.Average(s => s.QualityScore);
                if (avgQuality >= 80)
                {
                    importance += 10; // 高质量执行
                }
                else if (avgQuality >= 60)
                {
                    importance += 5;
                }
            }
            
            // 限制范围 0-100
            return Math.Min(100, Math.Max(0, importance));
        }

        /// <summary>
        /// 优化3: 压缩和归档记忆到长期记忆知识库（放宽条件，失败任务也归档）
        /// </summary>
        private async Task CompressAndArchiveMemoriesAsync(
            string AppID,
            string SessionID,
            string MemberID,
            string ClawID,
            string originalTask,
            string finalResult,
            TaskPlanning taskPlanning,
            LargeModelConfig embeddingModelConfig)
        {
            try
            {
                // 优化3: 失败任务也归档（作为经验教训）
                bool isCompleted = taskPlanning.PlanningStatus == PlanningStatus.Completed;
                bool isFailed = taskPlanning.PlanningStatus == PlanningStatus.Failed;

                if (!isCompleted && !isFailed)
                {
                    // 只跳过未完成的任务（进行中、待处理等）
                    return;
                }

                int importance = CalculateMemoryImportance(taskPlanning, isCompleted);

                // 优化3: 动态重要性阈值（失败任务降低阈值）
                int threshold = _options.Memory.ArchiveImportanceThreshold;
                if (isFailed && _options.Memory.ArchiveFailedTasks)
                {
                    threshold = Math.Max(50, threshold - 20); // 失败任务降低阈值20分
                }

                if (importance < threshold)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY,
                        $" 任务重要性不足({importance}<{threshold}),跳过归档");
                    return;
                }

                // 提取知识类型（失败任务标记为失败经验）
                string knowledgeType = isFailed ? "failed_experience" : DetermineKnowledgeType(originalTask, taskPlanning);

                // 提取主题
                string topic = ExtractTopic(originalTask);

                // 生成摘要
                string summary = GenerateSummary(originalTask, finalResult, taskPlanning);

                // 生成详细内容
                string content = GenerateDetailedContent(originalTask, finalResult, taskPlanning);

                // 生成向量嵌入
                string embeddingJson = string.Empty;
                if (embeddingModelConfig != null && embeddingModelConfig.Model != null)
                {
                    try
                    {
                        var embeddingVector = await _kernelService.GenerateEmbeddingAsync(
                            embeddingModelConfig.Model,
                            summary); // 使用摘要生成向量

                        embeddingJson = JsonConvert.SerializeObject(embeddingVector);
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, " 向量嵌入已生成");
                    }
                    catch (Exception ex)
                    {
                        LoggerHelper.LogWarning(_logger, ClawLogModules.MEMORY,
                            $" 生成向量嵌入失败: {ex.Message}，将继续保存但不包含向量");
                    }
                }

                // 创建长期记忆
                var longTermMemory = new LongTermMemoryInfo
                {
                    MemoryID = Guid.NewGuid().ToString(),
                    AppID = AppID,
                    ClawID = ClawID,
                    SessionID = SessionID,
                    MemberID = MemberID,
                    KnowledgeType = knowledgeType,
                    Topic = topic,
                    Summary = summary,
                    Content = content,
                    Embedding = embeddingJson,
                    Importance = importance,
                    AccessCount = 0,
                    SourceType = "episodic",
                    SourceID = taskPlanning.PlanningID,
                    Metadata = JsonConvert.SerializeObject(new
                    {
                        OriginalTask = originalTask,
                        TotalSteps = taskPlanning.TotalSteps,
                        Strategy = taskPlanning.Metadata.Strategy,
                        ExecutionTime = taskPlanning.Metadata.ActualDuration
                    }),
                    CreateTime = DateTime.Now,
                    LastUpdateTime = DateTime.Now
                };

                // 保存到长期记忆知识库
                string memoryId = LongTermMemoryBusiness.Add(longTermMemory);
                
                if (!string.IsNullOrEmpty(memoryId))
                {
                    LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                        $"[Memory] 长期记忆已归档 - MemoryID: {memoryId}, 类型: {knowledgeType}, 主题: {topic}, 重要性: {importance}");
                }
                
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Memory] 压缩和归档记忆失败");
            }
        }

        /// <summary>
        /// 优化2: 确定知识类型（增强版：同时分析用户提问和AI回答）
        /// </summary>
        private string DetermineKnowledgeType(string task, TaskPlanning taskPlanning)
        {
            // 获取AI回答内容（从步骤结果中提取）
            string result = string.Empty;
            if (taskPlanning.Steps != null && taskPlanning.Steps.Count > 0)
            {
                var lastStep = taskPlanning.Steps.LastOrDefault();
                result = lastStep?.ActualOutput ?? string.Empty;
            }
            
            // 1. 概念定义检测（同时检查提问和回答）
            if (task.Contains("是什么") || task.Contains("什么是") || task.Contains("定义") || 
                task.Contains("概念") || task.Contains("介绍") || task.Contains("解释") ||
                result.Contains("是指") || result.Contains("定义为") || result.Contains("概念是"))
            {
                return "concept";
            }
            
            // 2. 操作流程检测（同时检查提问和回答）
            if (task.Contains("如何") || task.Contains("怎么") || task.Contains("怎样") || 
                task.Contains("步骤") || task.Contains("方法") ||
                result.Contains("步骤如下") || result.Contains("首先") && result.Contains("然后") ||
                result.Contains("第一") || result.Contains("第二"))
            {
                return "procedure";
            }
            
            // 3. 问答对检测
            if (task.Contains("?") || task.Contains("？") || 
                task.Contains("吗") || task.Contains("呢"))
            {
                return "qa";
            }
            
            // 4. 经验总结检测（复杂任务或长回答）
            if ((taskPlanning.Steps != null && taskPlanning.Steps.Count > 3) ||
                result.Length > 500)
            {
                return "experience";
            }
            
            // 5. 用户偏好检测
            if (task.Contains("喜欢") || task.Contains("不喜欢") || 
                task.Contains("偏好") || task.Contains("习惯"))
            {
                return "preference";
            }
            
            // 6. 默认为事实信息
            return "fact";
        }

        /// <summary>
        /// 提取主题
        /// </summary>
        private string ExtractTopic(string task)
        {
            // 简单提取:取任务的前30个字符作为主题
            if (task.Length <= 30)
            {
                return task;
            }
            else
            {
                return task.Substring(0, 30) + "...";
            }
        }

        /// <summary>
        /// 生成摘要
        /// </summary>
        private string GenerateSummary(string task, string result, TaskPlanning taskPlanning)
        {
            return $"任务: {task.Substring(0, Math.Min(100, task.Length))}... " +
                   $"结果: {(result.Length > 100 ? result.Substring(0, 100) + "..." : result)} " +
                   $"(步骤数: {taskPlanning.TotalSteps}, 策略: {taskPlanning.Metadata.Strategy})";
        }

        /// <summary>
        /// 生成详细内容
        /// </summary>
        private string GenerateDetailedContent(string task, string result, TaskPlanning taskPlanning)
        {
            var content = new StringBuilder();
            content.AppendLine($"原始任务: {task}");
            content.AppendLine($"\n执行结果: {result}");
            content.AppendLine($"\n执行策略: {taskPlanning.Metadata.Strategy}");
            content.AppendLine($"总步骤数: {taskPlanning.TotalSteps}");
            content.AppendLine($"执行时间: {taskPlanning.Metadata.ActualDuration}秒");
            
            if (taskPlanning.Steps != null && taskPlanning.Steps.Count > 0)
            {
                content.AppendLine($"\n关键步骤:");
                foreach (var step in taskPlanning.Steps.Take(5)) // 只保留前5个步骤
                {
                    content.AppendLine($"- {step.StepType}: {step.StepDescription}");
                }
            }
            
            return content.ToString();
        }

        /// <summary>
        /// 自动知识提炼
        /// 从对话历史中提取知识并更新到长期记忆
        /// </summary>
        private async Task AutoExtractKnowledgeAsync(
            string AppID,
            string ClawID,
            string SessionID,
            string MemberID,
            string originalTask,
            string finalResult,
            MemoryContext memoryContext,
            LargeModelConfig embeddingModelConfig)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, "[AutoExtract] 开始自动知识提炼");

                // 优化1: 检查对话总长度是否足够（避免遗漏简短但有价值的对话）
                int totalLength = originalTask.Length + finalResult.Length;
                if (totalLength < _options.Memory.MinDialogueLengthForExtraction)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                        $"[AutoExtract] 对话总长度不足({totalLength}),跳过提炼");
                    return;
                }
                
                // 特殊情况：即使用户提问很短，只要AI回答足够长就提取（因为AI回答包含知识）
                if (finalResult.Length < 20 && totalLength < _options.Memory.MinDialogueLengthForExtraction * 2)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                        $"[AutoExtract] AI回答过短({finalResult.Length}),跳过提炼");
                    return;
                }

                // 构建对话上下文
                var context = new DialogueContext
                {
                    ChatHistory = memoryContext.WorkingMemory ?? new List<AppChatLogInfo>(),
                    UserProfile = memoryContext.UserProfile,
                    RelevantMemories = memoryContext.RelevantMemories
                };

                // 优化4: 提取知识（LLM优先，失败时降级到规则提取）
                List<ExtractedKnowledge> extractedKnowledge = null;
                
                try
                {
                    // 优先使用LLM提取
                    extractedKnowledge = await _knowledgeExtractionService.ExtractKnowledgeFromDialogueAsync(
                        originalTask, finalResult, context);
                }
                catch (Exception ex)
                {
                    if (_options.Memory.EnableRuleFallback)
                    {
                        LoggerHelper.LogWarning(_logger, ClawLogModules.MEMORY, 
                            $"[AutoExtract] LLM提取失败，使用规则降级: {ex.Message}");
                        
                        // 降级：使用规则提取
                        extractedKnowledge = ExtractKnowledgeByRules(originalTask, finalResult, memoryContext);
                    }
                    else
                    {
                        throw;
                    }
                }

                if (extractedKnowledge == null || extractedKnowledge.Count == 0)
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, "[AutoExtract] 未提取到有价值的知识");
                    return;
                }

                // 更新到长期记忆
                int updatedCount = 0;
                foreach (var knowledge in extractedKnowledge)
                {
                    var memoryId = await _knowledgeExtractionService.UpdateLongTermMemoryAsync(
                        knowledge, AppID, ClawID, SessionID, MemberID, embeddingModelConfig);

                    if (!string.IsNullOrEmpty(memoryId))
                    {
                        updatedCount++;
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                    $"[AutoExtract] 知识提炼完成 - 提取数量: {extractedKnowledge.Count}, 更新数量: {updatedCount}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[AutoExtract] 自动知识提炼失败");
            }
        }

        /// <summary>
        /// 优化4: 基于规则的知识提取（LLM降级方案）
        /// </summary>
        private List<ExtractedKnowledge> ExtractKnowledgeByRules(
            string userQuestion, 
            string aiAnswer, 
            MemoryContext context)
        {
            var knowledge = new List<ExtractedKnowledge>();
            
            try
            {
                // 规则1: 问答对检测
                if (userQuestion.Contains("什么") || userQuestion.Contains("如何") || 
                    userQuestion.Contains("怎么") || userQuestion.Contains("为什么") || 
                    userQuestion.Contains("是否") || userQuestion.Contains("能否") ||
                    userQuestion.Contains("?") || userQuestion.Contains("？"))
                {
                    knowledge.Add(new ExtractedKnowledge
                    {
                        Type = KnowledgeType.QA,
                        Topic = ExtractTopicEnhanced(userQuestion),
                        Summary = $"Q: {userQuestion.Substring(0, Math.Min(50, userQuestion.Length))}",
                        Content = $"Q: {userQuestion}\nA: {aiAnswer}",
                        Keywords = ExtractKeywords(userQuestion + " " + aiAnswer),
                        Importance = 70,
                        Confidence = 0.8,
                        SourceDialogue = $"{userQuestion} -> {aiAnswer.Substring(0, Math.Min(100, aiAnswer.Length))}"
                    });
                }
                
                // 规则2: 概念定义检测
                if (userQuestion.Contains("是什么") || userQuestion.Contains("什么是") || 
                    userQuestion.Contains("定义") || userQuestion.Contains("概念") ||
                    userQuestion.Contains("介绍") || userQuestion.Contains("解释"))
                {
                    knowledge.Add(new ExtractedKnowledge
                    {
                        Type = KnowledgeType.Concept,
                        Topic = ExtractConceptName(userQuestion),
                        Summary = aiAnswer.Substring(0, Math.Min(100, aiAnswer.Length)),
                        Content = aiAnswer,
                        Keywords = ExtractKeywords(userQuestion + " " + aiAnswer),
                        Importance = 80,
                        Confidence = 0.9,
                        SourceDialogue = $"{userQuestion} -> {aiAnswer.Substring(0, Math.Min(100, aiAnswer.Length))}"
                    });
                }
                
                // 规则3: 流程步骤检测
                if (userQuestion.Contains("如何") || userQuestion.Contains("怎么") || userQuestion.Contains("怎样") ||
                    aiAnswer.Contains("步骤") || aiAnswer.Contains("第一") || aiAnswer.Contains("第二") ||
                    (aiAnswer.Contains("首先") && aiAnswer.Contains("然后")))
                {
                    knowledge.Add(new ExtractedKnowledge
                    {
                        Type = KnowledgeType.Procedure,
                        Topic = ExtractTopicEnhanced(userQuestion),
                        Summary = $"操作流程: {userQuestion}",
                        Content = aiAnswer,
                        Keywords = ExtractKeywords(userQuestion + " " + aiAnswer),
                        Importance = 75,
                        Confidence = 0.85,
                        SourceDialogue = $"{userQuestion} -> {aiAnswer.Substring(0, Math.Min(100, aiAnswer.Length))}"
                    });
                }
                
                // 规则4: 用户偏好检测
                if (userQuestion.Contains("喜欢") || userQuestion.Contains("不喜欢") || 
                    userQuestion.Contains("偏好") || userQuestion.Contains("习惯"))
                {
                    knowledge.Add(new ExtractedKnowledge
                    {
                        Type = KnowledgeType.Preference,
                        Topic = "用户偏好",
                        Summary = userQuestion,
                        Content = aiAnswer,
                        Keywords = ExtractKeywords(userQuestion),
                        Importance = 65,
                        Confidence = 0.75,
                        SourceDialogue = $"{userQuestion} -> {aiAnswer.Substring(0, Math.Min(100, aiAnswer.Length))}"
                    });
                }
                
                LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                    $"[RuleExtract] 规则提取完成 - 提取数量: {knowledge.Count}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[RuleExtract] 规则提取失败");
            }
            
            return knowledge;
        }

        /// <summary>
        /// 优化6: 增强的主题提取（智能提取关键词）
        /// </summary>
        private string ExtractTopicEnhanced(string task)
        {
            try
            {
                // 1. 移除疑问词和标点
                var cleanTask = task.Replace("什么是", "")
                                   .Replace("是什么", "")
                                   .Replace("如何", "")
                                   .Replace("怎么", "")
                                   .Replace("怎样", "")
                                   .Replace("为什么", "")
                                   .Replace("?", "")
                                   .Replace("？", "")
                                   .Replace("吗", "")
                                   .Replace("呢", "")
                                   .Trim();
                
                // 2. 如果清理后为空，使用原始任务
                if (string.IsNullOrWhiteSpace(cleanTask))
                {
                    cleanTask = task;
                }
                
                // 3. 提取第一个有意义的词组（长度>2的词）
                var words = cleanTask.Split(new[] { ' ', '，', ',', '、', '的', '了', '在' }, 
                    StringSplitOptions.RemoveEmptyEntries);
                
                var topic = words.FirstOrDefault(w => w.Length > 1 && !IsStopWord(w));
                
                // 4. 限制长度
                if (string.IsNullOrEmpty(topic))
                {
                    topic = cleanTask.Substring(0, Math.Min(30, cleanTask.Length));
                }
                else if (topic.Length > 30)
                {
                    topic = topic.Substring(0, 30);
                }
                
                return topic;
            }
            catch
            {
                // 降级：使用原始方法
                return ExtractTopic(task);
            }
        }

        /// <summary>
        /// 提取概念名称
        /// </summary>
        private string ExtractConceptName(string task)
        {
            // 提取"是什么"、"什么是"后面的词
            if (task.Contains("是什么"))
            {
                var parts = task.Split(new[] { "是什么" }, StringSplitOptions.None);
                if (parts.Length > 0)
                {
                    return parts[0].Trim().Substring(0, Math.Min(30, parts[0].Trim().Length));
                }
            }
            else if (task.Contains("什么是"))
            {
                var parts = task.Split(new[] { "什么是" }, StringSplitOptions.None);
                if (parts.Length > 1)
                {
                    return parts[1].Trim().Substring(0, Math.Min(30, parts[1].Trim().Length));
                }
            }
            
            return ExtractTopicEnhanced(task);
        }

        /// <summary>
        /// 提取关键词
        /// </summary>
        private List<string> ExtractKeywords(string text)
        {
            var keywords = new List<string>();
            
            try
            {
                // 简单分词（按空格、标点分割）
                var words = text.Split(new[] { ' ', '，', ',', '。', '.', '、', '?', '？', '!', '！', '\n', '\r' }, 
                    StringSplitOptions.RemoveEmptyEntries);
                
                // 过滤停用词和短词，取前10个
                keywords = words
                    .Where(w => w.Length > 1 && !IsStopWord(w))
                    .Distinct()
                    .Take(10)
                    .ToList();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[ExtractKeywords] 关键词提取失败");
            }
            
            return keywords;
        }

        /// <summary>
        /// 判断是否为停用词
        /// </summary>
        private bool IsStopWord(string word)
        {
            var stopWords = new HashSet<string>
            {
                "的", "了", "在", "是", "我", "你", "他", "她", "它", "们",
                "这", "那", "有", "个", "和", "与", "或", "但", "而", "就",
                "都", "要", "会", "能", "可以", "不", "没", "吗", "呢", "啊",
                "the", "a", "an", "and", "or", "but", "is", "are", "was", "were"
            };
            
            return stopWords.Contains(word.ToLower());
        }

        /// <summary>
        /// 优化5: 判断是否应该触发知识去重合并
        /// </summary>
        private bool ShouldTriggerMerge(string AppID, string MemberID)
        {
            try
            {
                // 策略1: 每N次对话触发一次（基于用户画像的交互次数）
                var userProfile = UserProfileBusiness.GetByMemberAndApp(MemberID, AppID);
                if (userProfile != null && userProfile.TotalInteractions > 0)
                {
                    if (userProfile.TotalInteractions % _options.Memory.AutoMergeFrequency == 0)
                    {
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                            $"[AutoMerge] 触发条件: 交互次数达到{userProfile.TotalInteractions}");
                        return true;
                    }
                }

                // 策略2: 定期触发（每24小时）
                // 这里简化实现：通过检查长期记忆的最后更新时间
                var recentMemories = LongTermMemoryBusiness.GetByMemberAndApp(MemberID, AppID, 1);
                if (recentMemories != null && recentMemories.Count > 0)
                {
                    var lastMemory = recentMemories.First();
                    var hoursSinceLastUpdate = (DateTime.Now - lastMemory.LastUpdateTime).TotalHours;
                    
                    if (hoursSinceLastUpdate >= _options.Memory.AutoMergePeriodHours)
                    {
                        LoggerHelper.LogInfo(_logger, ClawLogModules.MEMORY, 
                            $"[AutoMerge] 触发条件: 距上次更新{hoursSinceLastUpdate:F1}小时");
                        return true;
                    }
                }

                return false;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[AutoMerge] 判断触发条件失败");
                return false;
            }
        }

        #endregion
    }
}
