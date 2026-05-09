using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Entity.Model;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Models;
using ZSN.AI.Node.Claw.Utils;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 智能主控服务实现
    /// 使用LLM理解上下文和系统提示词，智能判断是否需要任务规划
    /// </summary>
    public class MasterControlService : IMasterControlService
    {
        private readonly IChatService _chatService;
        private readonly ILogger<MasterControlService> _logger;
        private readonly ClawAIOptions _options;

        // 缓存：key = hash(userInput + systemPrompt), value = (result, timestamp)
        private readonly ConcurrentDictionary<string, (MasterControlResult result, DateTime timestamp)> _cache;
        
        // 统计信息
        private long _totalRequests = 0;
        private long _cacheHits = 0;

        public MasterControlService(
            IChatService chatService,
            ILogger<MasterControlService> logger,
            IOptions<ClawAIOptions> options)
        {
            _chatService = chatService ?? throw new ArgumentNullException(nameof(chatService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _options = options?.Value ?? new ClawAIOptions();

            _cache = new ConcurrentDictionary<string, (MasterControlResult, DateTime)>();
        }

        /// <summary>
        /// 判断用户输入应该直接回复还是进行任务规划
        /// </summary>
        public async Task<MasterControlResult> DecideAsync(MasterControlContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            _totalRequests++;
            var stopwatch = Stopwatch.StartNew();

            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.MASTER_CONTROL, 
                    $"开始主控判断 - UserInput: {context.UserInput?.Substring(0, Math.Min(50, context.UserInput?.Length ?? 0))}...");

                // 检查缓存
                if (_options.MasterControl.EnableCache)
                {
                    var cacheKey = GenerateCacheKey(context);
                    if (_cache.TryGetValue(cacheKey, out var cached))
                    {
                        var age = DateTime.UtcNow - cached.timestamp;
                        if (age.TotalMinutes < _options.MasterControl.CacheExpirationMinutes)
                        {
                            _cacheHits++;
                            cached.result.FromCache = true;
                            cached.result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                            
                            LoggerHelper.LogInfo(_logger, ClawLogModules.MASTER_CONTROL, 
                                $"缓存命中 - Decision: {cached.result.Decision}, Reason: {cached.result.Reason}");
                            
                            return cached.result;
                        }
                        else
                        {
                            // 缓存过期，移除
                            _cache.TryRemove(cacheKey, out _);
                        }
                    }
                }

                // 调用LLM进行判断
                var result = await CallLLMForDecisionAsync(context);
                result.ElapsedMilliseconds = stopwatch.ElapsedMilliseconds;
                result.FromCache = false;

                // 保存到缓存
                if (_options.MasterControl.EnableCache && result.Confidence >= 80)
                {
                    var cacheKey = GenerateCacheKey(context);
                    _cache[cacheKey] = (result, DateTime.UtcNow);
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.MASTER_CONTROL, 
                    $"主控判断完成 - Decision: {result.Decision}, Confidence: {result.Confidence}%, Reason: {result.Reason}, Elapsed: {result.ElapsedMilliseconds}ms");

                return result;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.MASTER_CONTROL, 
                    $"主控判断失败: {ex.Message}", ex);

                // 回退策略
                return GetFallbackResult(context);
            }
        }

        /// <summary>
        /// 调用LLM进行决策
        /// </summary>
        private async Task<MasterControlResult> CallLLMForDecisionAsync(MasterControlContext context)
        {
            // 使用上下文中的模型配置
            var modelConfig = context.ModelConfig;
            if (modelConfig == null)
            {
                throw new ArgumentException("ModelConfig is required in MasterControlContext", nameof(context));
            }

            // 构建提示词
            var prompt = BuildPrompt(context);

            // 创建ChatHistory
            var chatHistory = new ChatHistory();
            chatHistory.AddSystemMessage(prompt);
            chatHistory.AddUserMessage(context.UserInput);

            // 调用LLM（使用流式响应收集完整结果）
            var responseBuilder = new StringBuilder();
            
            try
            {
                await foreach (var chunk in _chatService.SendChatAsync(
                    modelConfig,
                    chatHistory,
                    Function: null,
                    responseFormat: "text",
                    enableStreamingObservation: false,
                    progress: null,
                    ct: System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.MASTER_CONTROL, 
                    $"LLM调用失败: {ex.Message}", ex);
                throw;
            }

            var llmResponse = responseBuilder.ToString();
            
            LoggerHelper.LogDebug(_logger, ClawLogModules.MASTER_CONTROL, 
                $"LLM原始响应: {llmResponse}");

            // 解析LLM响应
            return ParseLLMResponse(llmResponse);
        }

        /// <summary>
        /// 构建提示词
        /// </summary>
        private string BuildPrompt(MasterControlContext context)
        {
            // 从context中获取提示词模板，如果为空则使用默认模板
            var promptTemplate = context.PromptTemplate;
            if (string.IsNullOrEmpty(promptTemplate))
            {
                _logger.LogWarning("[MasterControl] 未提供提示词模板，使用默认模板");
                promptTemplate = GetDefaultPromptTemplate();
            }

            var prompt = promptTemplate
                .Replace("{{SystemPrompt}}", context.SystemPrompt ?? "未提供系统提示词")
                .Replace("{{AvailableWorkflows}}", context.AvailableWorkflows ?? "无可用WorkFlow")
                .Replace("{{ChatHistory}}", context.ChatHistory ?? "无对话历史")
                .Replace("{{UserProfileSummary}}", context.UserProfileSummary ?? "无用户画像");

            return prompt;
        }

        /// <summary>
        /// 解析LLM响应
        /// </summary>
        private MasterControlResult ParseLLMResponse(string llmResponse)
        {
            try
            {
                // 提取JSON部分（可能包含markdown代码块）
                var jsonContent = ExtractJsonFromResponse(llmResponse);

                // 解析JSON
                var jsonObj = JsonConvert.DeserializeObject<dynamic>(jsonContent);

                var decisionStr = (string)jsonObj.decision;
                var decision = decisionStr.Equals("DirectResponse", StringComparison.OrdinalIgnoreCase)
                    ? MasterControlDecision.DirectResponse
                    : MasterControlDecision.TaskPlanning;

                return new MasterControlResult
                {
                    Decision = decision,
                    Reason = (string)jsonObj.reason ?? "未提供理由",
                    Confidence = (int?)jsonObj.confidence ?? 80,
                    SuggestedResponseStrategy = (string)jsonObj.suggestedResponseStrategy ?? "friendly",
                    DirectResponseContent = (string)jsonObj.directResponseContent ?? ""
                };
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.MASTER_CONTROL, 
                    $"解析LLM响应失败: {ex.Message}, 原始响应: {llmResponse}", ex);

                // 解析失败，使用启发式规则
                return ParseWithHeuristics(llmResponse);
            }
        }

        /// <summary>
        /// 从响应中提取JSON内容
        /// </summary>
        private string ExtractJsonFromResponse(string response)
        {
            // 移除markdown代码块标记
            response = response.Trim();
            
            if (response.StartsWith("```json"))
            {
                response = response.Substring(7);
            }
            else if (response.StartsWith("```"))
            {
                response = response.Substring(3);
            }

            if (response.EndsWith("```"))
            {
                response = response.Substring(0, response.Length - 3);
            }

            return response.Trim();
        }

        /// <summary>
        /// 使用启发式规则解析（当JSON解析失败时）
        /// </summary>
        private MasterControlResult ParseWithHeuristics(string response)
        {
            var responseLower = response.ToLower();

            if (responseLower.Contains("directresponse") || responseLower.Contains("直接回复"))
            {
                return MasterControlResult.CreateDirectResponse(
                    "基于启发式规则判断：响应中包含DirectResponse关键词",
                    confidence: 70);
            }
            else if (responseLower.Contains("taskplanning") || responseLower.Contains("任务规划"))
            {
                return MasterControlResult.CreateTaskPlanning(
                    "基于启发式规则判断：响应中包含TaskPlanning关键词",
                    confidence: 70);
            }
            else
            {
                // 默认使用回退策略
                LoggerHelper.LogWarning(_logger, ClawLogModules.MASTER_CONTROL, 
                    "无法解析LLM响应，使用回退策略");
                
                return _options.MasterControl.FallbackStrategy == "fallback_to_direct"
                    ? MasterControlResult.CreateDirectResponse("解析失败，使用回退策略", confidence: 50)
                    : MasterControlResult.CreateTaskPlanning("解析失败，使用回退策略", confidence: 50);
            }
        }

        /// <summary>
        /// 获取回退结果（当主控判断失败时）
        /// </summary>
        private MasterControlResult GetFallbackResult(MasterControlContext context)
        {
            if (_options.MasterControl.FallbackStrategy == "fallback_to_direct")
            {
                return MasterControlResult.CreateDirectResponse(
                    "主控判断失败，使用回退策略：直接回复",
                    confidence: 50);
            }
            else
            {
                return MasterControlResult.CreateTaskPlanning(
                    "主控判断失败，使用回退策略：任务规划",
                    confidence: 50);
            }
        }

        /// <summary>
        /// 生成缓存键
        /// </summary>
        private string GenerateCacheKey(MasterControlContext context)
        {
            var key = $"{context.UserInput}|{context.SystemPrompt}|{context.ChatHistory}";
            return key.GetHashCode().ToString();
        }

        /// <summary>
        /// 清除缓存
        /// </summary>
        public void ClearCache()
        {
            _cache.Clear();
            LoggerHelper.LogInfo(_logger, ClawLogModules.MASTER_CONTROL, "缓存已清除");
        }

        /// <summary>
        /// 获取缓存统计信息
        /// </summary>
        public string GetCacheStats()
        {
            var hitRate = _totalRequests > 0 ? (_cacheHits * 100.0 / _totalRequests) : 0;
            return $"总请求: {_totalRequests}, 缓存命中: {_cacheHits}, 命中率: {hitRate:F2}%, 缓存大小: {_cache.Count}";
        }

        /// <summary>
        /// 获取默认提示词模板
        /// </summary>
        private string GetDefaultPromptTemplate()
        {
            return @"你是一个智能主控系统，负责判断用户输入应该直接回复还是进行任务规划。

系统能力：{{SystemPrompt}}
可用WorkFlow：{{AvailableWorkflows}}
对话历史：{{ChatHistory}}

请判断用户输入应该：
1. DirectResponse - 直接回复（问候、感谢、简单问答）
2. TaskPlanning - 任务规划（复杂任务、需要WorkFlow）

输出JSON格式：
{
  ""decision"": ""DirectResponse 或 TaskPlanning"",
  ""reason"": ""判断理由"",
  ""confidence"": 85,
  ""suggestedResponseStrategy"": ""friendly""
}";
        }
    }
}
