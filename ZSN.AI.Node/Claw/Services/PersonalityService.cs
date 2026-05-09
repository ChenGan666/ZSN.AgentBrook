using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Utils;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// AI 个性服务实现
    /// 管理 AI 的个性特征、情绪状态和个性化响应生成
    /// </summary>
    public class PersonalityService : IPersonalityService
    {
        private readonly ILogger<PersonalityService> _logger;
        private readonly IMemoryService _memoryService;

        public PersonalityService(
            ILogger<PersonalityService> logger,
            IMemoryService memoryService)
        {
            _logger = logger;
            _memoryService = memoryService;
        }

        public async Task<AIPersonalityState> InitializePersonalityAsync(
            string SessionID, 
            string AppID, 
            PersonalityConfig config)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, $" 初始化个性状态 - SessionID: {SessionID}");

                // 尝试从数据库加载现有状态
                var aiState = await _memoryService.LoadAIPersonalityStateAsync(SessionID, config);

                // 如果是新状态,初始化默认个性特征
                if (aiState.PersonalityTraits == null || aiState.PersonalityTraits.Count == 0)
                {
                    aiState.AppID = AppID;
                    aiState.SessionID = SessionID;
                    aiState.PersonalityTraits = CreateDefaultPersonalityTraits(config);
                    aiState.EmotionalState = CreateDefaultEmotionalState(config);
                    aiState.CurrentGoals = CreateDefaultGoals(config);

                    // 保存到数据库
                    await SavePersonalityStateAsync(aiState);

                    LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, " 创建新的个性状态");
                }
                else
                {
                    LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, " 加载已存在的个性状态");
                }

                return aiState;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.PERSONALITY, $" 初始化个性状态失败 - SessionID: {SessionID}");
                // 返回默认状态
                return new AIPersonalityState
                {
                    SessionID = SessionID,
                    AppID = AppID,
                    PersonalityTraits = CreateDefaultPersonalityTraits(config),
                    EmotionalState = CreateDefaultEmotionalState(config),
                    CurrentGoals = CreateDefaultGoals(config)
                };
            }
        }

        public async Task<string> ApplyPersonalityToPromptAsync(
            string originalPrompt, 
            AIPersonalityState aiState, 
            PersonalityConfig config)
        {
            try
            {
                if (!config.enabled || aiState == null)
                {
                    return originalPrompt;
                }

                var personalitySection = new StringBuilder();

                // 1. 添加个性描述
                if (!string.IsNullOrEmpty(config.personalityDescription))
                {
                    personalitySection.AppendLine($"\n## AI 个性特征");
                    personalitySection.AppendLine(config.personalityDescription);
                }

                // 2. 添加个性特征
                if (aiState.PersonalityTraits != null && aiState.PersonalityTraits.Count > 0)
                {
                    personalitySection.AppendLine($"\n## 当前个性状态");
                    foreach (var trait in aiState.PersonalityTraits)
                    {
                        personalitySection.AppendLine($"- {trait.Key}: {trait.Value}");
                    }
                }

                // 3. 添加情绪状态 (如果启用)
                if (config.enableEmotionalState && aiState.EmotionalState != null && aiState.EmotionalState.Count > 0)
                {
                    personalitySection.AppendLine($"\n## 当前情绪状态");
                    foreach (var emotion in aiState.EmotionalState)
                    {
                        personalitySection.AppendLine($"- {emotion.Key}: {emotion.Value}");
                    }
                    personalitySection.AppendLine("\n请根据当前情绪状态调整你的回复语气和风格。");
                }

                // 4. 添加目标 (如果启用)
                if (config.enableGoalOriented && aiState.CurrentGoals != null && aiState.CurrentGoals.Count > 0)
                {
                    personalitySection.AppendLine($"\n## 当前目标");
                    for (int i = 0; i < aiState.CurrentGoals.Count; i++)
                    {
                        personalitySection.AppendLine($"{i + 1}. {aiState.CurrentGoals[i]}");
                    }
                    personalitySection.AppendLine("\n在回复时请考虑这些目标,并尝试推进目标的达成。");
                }

                // 将个性信息插入到原始提示词中
                var enhancedPrompt = originalPrompt + "\n" + personalitySection.ToString();

                LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, $" 应用个性化到提示词 - 添加了 {personalitySection.Length} 字符");

                await Task.CompletedTask;
                return enhancedPrompt;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Personality] 应用个性化失败");
                return originalPrompt;
            }
        }

        public async Task<AIPersonalityState> UpdateEmotionalStateAsync(
            AIPersonalityState aiState, 
            bool interactionSuccess, 
            string userFeedback = null,
            PersonalityConfig config = null)
        {
            try
            {
                if (config == null || !config.enableEmotionalState || aiState == null)
                {
                    return aiState;
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, $" 更新情绪状态 - Success: {interactionSuccess}");

                // 获取当前情绪值
                var confidence = GetEmotionValue(aiState, "confidence", 50);
                var satisfaction = GetEmotionValue(aiState, "satisfaction", 50);
                var engagement = GetEmotionValue(aiState, "engagement", 50);

                // 根据交互结果调整情绪
                if (interactionSuccess)
                {
                    confidence = Math.Min(100, confidence + 5);
                    satisfaction = Math.Min(100, satisfaction + 5);
                    engagement = Math.Min(100, engagement + 3);
                }
                else
                {
                    confidence = Math.Max(0, confidence - 3);
                    satisfaction = Math.Max(0, satisfaction - 5);
                    engagement = Math.Max(0, engagement - 2);
                }

                // 根据用户反馈进一步调整
                if (!string.IsNullOrEmpty(userFeedback))
                {
                    var feedbackLower = userFeedback.ToLower();
                    if (feedbackLower.Contains("好") || feedbackLower.Contains("棒") || feedbackLower.Contains("excellent"))
                    {
                        satisfaction = Math.Min(100, satisfaction + 10);
                        engagement = Math.Min(100, engagement + 5);
                    }
                    else if (feedbackLower.Contains("差") || feedbackLower.Contains("不好") || feedbackLower.Contains("bad"))
                    {
                        satisfaction = Math.Max(0, satisfaction - 10);
                        confidence = Math.Max(0, confidence - 5);
                    }
                }

                // 更新情绪状态
                aiState.EmotionalState["confidence"] = confidence;
                aiState.EmotionalState["satisfaction"] = satisfaction;
                aiState.EmotionalState["engagement"] = engagement;
                aiState.EmotionalState["lastUpdate"] = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");

                // 保存到数据库
                await SavePersonalityStateAsync(aiState);

                LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                    $"[Personality] 情绪更新完成 - Confidence: {confidence}, Satisfaction: {satisfaction}, Engagement: {engagement}");

                return aiState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Personality] 更新情绪状态失败");
                return aiState;
            }
        }

        public async Task<AIPersonalityState> UpdateGoalsAsync(
            AIPersonalityState aiState, 
            bool taskCompleted, 
            string taskDescription,
            PersonalityConfig config = null)
        {
            try
            {
                if (config == null || !config.enableGoalOriented || aiState == null)
                {
                    return aiState;
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, $" 更新目标 - TaskCompleted: {taskCompleted}");

                // 如果任务完成,从目标列表中移除相关目标
                if (taskCompleted && !string.IsNullOrEmpty(taskDescription))
                {
                    aiState.CurrentGoals.RemoveAll(g => 
                        g.Contains(taskDescription, StringComparison.OrdinalIgnoreCase));
                    
                    LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, $" 移除已完成目标: {taskDescription}");
                }

                // 如果目标列表为空,添加默认目标
                if (aiState.CurrentGoals.Count == 0)
                {
                    aiState.CurrentGoals = CreateDefaultGoals(config);
                }

                // 保存到数据库
                await SavePersonalityStateAsync(aiState);

                return aiState;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Personality] 更新目标失败");
                return aiState;
            }
        }

        public async Task<string> GeneratePersonalizedPrefixAsync(
            AIPersonalityState aiState, 
            PersonalityConfig config)
        {
            try
            {
                if (!config.enabled || !config.enableEmotionalState || aiState?.EmotionalState == null)
                {
                    await Task.CompletedTask;
                    return string.Empty;
                }

                var confidence = GetEmotionValue(aiState, "confidence", 50);
                var satisfaction = GetEmotionValue(aiState, "satisfaction", 50);

                // 根据情绪状态生成不同的前缀
                if (confidence > 80 && satisfaction > 80)
                {
                    await Task.CompletedTask;
                    return "我很高兴能帮助你！";
                }
                else if (confidence > 60)
                {
                    await Task.CompletedTask;
                    return "让我来帮你解决这个问题。";
                }
                else if (confidence < 40)
                {
                    await Task.CompletedTask;
                    return "我会尽力帮助你,如果遇到困难请告诉我。";
                }

                await Task.CompletedTask;
                return string.Empty;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Personality] 生成个性化前缀失败");
                await Task.CompletedTask;
                return string.Empty;
            }
        }

        public async Task<decimal> UpdateSuccessRateAsync(
            string SessionID, 
            bool taskSuccess, 
            int qualityScore = 0)
        {
            try
            {
                var entity = AIPersonalityStateBusiness.GetBySessionID(SessionID);
                if (entity != null)
                {
                    // 计算新的成功率
                    var totalInteractions = entity.InteractionCount + 1;
                    var successCount = (entity.SuccessRate / 100m) * entity.InteractionCount;
                    
                    if (taskSuccess)
                    {
                        successCount += 1;
                    }
                    
                    var newSuccessRate = (successCount / totalInteractions) * 100;

                    // 如果提供了质量评分,进行加权计算
                    if (qualityScore > 0)
                    {
                        newSuccessRate = (newSuccessRate * 0.7m) + (qualityScore * 0.3m);
                    }

                    // 更新数据库
                    AIPersonalityStateBusiness.UpdateSuccessRate(entity.StateID, newSuccessRate);

                    LoggerHelper.LogInfo(_logger, "[REPLACE_MODULE]", 
                        $"[Personality] 更新成功率 - SessionID: {SessionID}, NewRate: {newSuccessRate:F2}%");

                    await Task.CompletedTask;
                    return newSuccessRate;
                }

                await Task.CompletedTask;
                return 0;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.PERSONALITY, $" 更新成功率失败 - SessionID: {SessionID}");
                await Task.CompletedTask;
                return 0;
            }
        }

        public async Task<string> GetPersonalizedSystemMessageAsync(
            string baseSystemMessage,
            AIPersonalityState aiState,
            PersonalityConfig config)
        {
            try
            {
                if (!config.enabled || aiState == null)
                {
                    return baseSystemMessage;
                }

                var systemMessage = new StringBuilder(baseSystemMessage);

                // 添加个性化系统指令
                systemMessage.AppendLine("\n\n## 个性化指令");

                if (!string.IsNullOrEmpty(config.personalityDescription))
                {
                    systemMessage.AppendLine($"\n你的个性特征: {config.personalityDescription}");
                }

                if (config.enableEmotionalState && aiState.EmotionalState != null)
                {
                    var confidence = GetEmotionValue(aiState, "confidence", 50);
                    var satisfaction = GetEmotionValue(aiState, "satisfaction", 50);

                    systemMessage.AppendLine($"\n当前情绪状态:");
                    systemMessage.AppendLine($"- 自信度: {confidence}%");
                    systemMessage.AppendLine($"- 满意度: {satisfaction}%");
                    systemMessage.AppendLine("\n请根据这些情绪状态调整你的回复风格和语气。");
                }

                if (config.enableGoalOriented && aiState.CurrentGoals != null && aiState.CurrentGoals.Count > 0)
                {
                    systemMessage.AppendLine($"\n你的当前目标:");
                    foreach (var goal in aiState.CurrentGoals)
                    {
                        systemMessage.AppendLine($"- {goal}");
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, " 生成个性化系统消息");

                await Task.CompletedTask;
                return systemMessage.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[Personality] 生成个性化系统消息失败");
                return baseSystemMessage;
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 创建默认个性特征
        /// </summary>
        private Dictionary<string, object> CreateDefaultPersonalityTraits(PersonalityConfig config)
        {
            var traits = new Dictionary<string, object>
            {
                { "friendliness", 80 },      // 友好度
                { "professionalism", 90 },   // 专业度
                { "creativity", 70 },        // 创造力
                { "patience", 85 },          // 耐心度
                { "humor", 50 }              // 幽默感
            };

            // 如果有自定义描述,可以根据描述调整特征值
            if (!string.IsNullOrEmpty(config.personalityDescription))
            {
                var desc = config.personalityDescription.ToLower();
                if (desc.Contains("专业") || desc.Contains("professional"))
                {
                    traits["professionalism"] = 95;
                }
                if (desc.Contains("友好") || desc.Contains("friendly"))
                {
                    traits["friendliness"] = 90;
                }
                if (desc.Contains("创意") || desc.Contains("creative"))
                {
                    traits["creativity"] = 85;
                }
                if (desc.Contains("幽默") || desc.Contains("humor"))
                {
                    traits["humor"] = 75;
                }
            }

            return traits;
        }

        /// <summary>
        /// 创建默认情绪状态
        /// </summary>
        private Dictionary<string, object> CreateDefaultEmotionalState(PersonalityConfig config)
        {
            return new Dictionary<string, object>
            {
                { "confidence", 70 },        // 自信度
                { "satisfaction", 70 },      // 满意度
                { "engagement", 70 },        // 参与度
                { "energy", 80 },            // 能量水平
                { "lastUpdate", DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") }
            };
        }

        /// <summary>
        /// 创建默认目标
        /// </summary>
        private List<string> CreateDefaultGoals(PersonalityConfig config)
        {
            return new List<string>
            {
                "提供准确和有用的信息",
                "理解用户的真实需求",
                "保持专业和友好的态度",
                "持续改进服务质量"
            };
        }

        /// <summary>
        /// 获取情绪值
        /// </summary>
        private int GetEmotionValue(AIPersonalityState aiState, string emotionKey, int defaultValue)
        {
            if (aiState?.EmotionalState == null || !aiState.EmotionalState.ContainsKey(emotionKey))
            {
                return defaultValue;
            }

            try
            {
                return Convert.ToInt32(aiState.EmotionalState[emotionKey]);
            }
            catch
            {
                return defaultValue;
            }
        }

        /// <summary>
        /// 保存个性状态到数据库
        /// </summary>
        private async Task SavePersonalityStateAsync(AIPersonalityState aiState)
        {
            try
            {
                var entity = AIPersonalityStateBusiness.GetBySessionID(aiState.SessionID);

                if (entity == null)
                {
                    // 创建新记录
                    entity = new AIPersonalityStateInfo
                    {
                        StateID = Guid.NewGuid().ToString(),
                        SessionID = aiState.SessionID,
                        AppID = aiState.AppID,
                        PersonalityTraits = JsonConvert.SerializeObject(aiState.PersonalityTraits ?? new Dictionary<string, object>()),
                        EmotionalState = JsonConvert.SerializeObject(aiState.EmotionalState ?? new Dictionary<string, object>()),
                        CurrentGoals = JsonConvert.SerializeObject(aiState.CurrentGoals ?? new List<string>()),
                        InteractionCount = 0,
                        SuccessRate = 0,
                        CreateTime = DateTime.Now,
                        LastUpdateTime = DateTime.Now
                    };

                    AIPersonalityStateBusiness.Add(entity);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, $" 创建新的个性状态记录 - SessionID: {aiState.SessionID}");
                }
                else
                {
                    // 更新现有记录
                    entity.PersonalityTraits = JsonConvert.SerializeObject(aiState.PersonalityTraits ?? new Dictionary<string, object>());
                    entity.EmotionalState = JsonConvert.SerializeObject(aiState.EmotionalState ?? new Dictionary<string, object>());
                    entity.CurrentGoals = JsonConvert.SerializeObject(aiState.CurrentGoals ?? new List<string>());
                    entity.LastUpdateTime = DateTime.Now;

                    AIPersonalityStateBusiness.Update(entity);
                    LoggerHelper.LogInfo(_logger, ClawLogModules.PERSONALITY, $" 更新个性状态记录 - SessionID: {aiState.SessionID}");
                }

                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.PERSONALITY, $" 保存个性状态失败 - SessionID: {aiState.SessionID}");
            }
        }

        #endregion
    }
}
