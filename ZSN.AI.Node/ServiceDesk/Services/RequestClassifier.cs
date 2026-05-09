using System.Diagnostics;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ZSN.AI.Entity;
using ZSN.AI.Node.ServiceDesk.Interfaces;
using ZSN.AI.Node.ServiceDesk.Models;

namespace ZSN.AI.Node.ServiceDesk.Services
{
    /// <summary>
    /// 请求分类器 — 快速判断用户消息类型，决定处理路径
    /// </summary>
    public class RequestClassifier : IRequestClassifier
    {
        private readonly ILogger<RequestClassifier> _logger;

        public RequestClassifier(ILogger<RequestClassifier> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 分类用户请求，决定处理策略
        /// </summary>
        public async Task<ClassificationResult> ClassifyRequestAsync(
            string userMessage,
            MemoryContext memoryContext,
            ServiceDeskData config)
        {
            var stopwatch = Stopwatch.StartNew();
            var traceBuilder = new List<string>();

            // 1. 快速路径：问候语检测
            if (IsGreeting(userMessage, config))
            {
                traceBuilder.Add("Greeting matched");
                return BuildResult(MessageType.Greeting, null, 1.0,
                    MessageComplexity.Simple, ProcessingStrategy.DirectReply, traceBuilder, stopwatch);
            }

            // 2. 快速路径：闲聊检测
            if (IsSmallTalk(userMessage, config))
            {
                traceBuilder.Add("SmallTalk matched");
                return BuildResult(MessageType.SmallTalk, null, 1.0,
                    MessageComplexity.Simple, ProcessingStrategy.DirectReply, traceBuilder, stopwatch);
            }

            // 3. 意图检测（基于配置的 IntentRule）
            var intentResult = DetectIntent(userMessage, config.IntentRules);
            if (intentResult.Confidence > 0)
            {
                traceBuilder.Add($"Intent={intentResult.IntentName}, Confidence={intentResult.Confidence:F2}");
            }

            // 4. 复杂度评估
            var complexity = EvaluateComplexity(userMessage, intentResult, memoryContext);
            traceBuilder.Add($"Complexity={complexity}");

            // 5. 策略决策
            var strategy = DetermineStrategy(intentResult, complexity, config);
            traceBuilder.Add($"Strategy={strategy}");

            var type = DetermineMessageType(intentResult);
            return BuildResult(type, intentResult.IntentName, intentResult.Confidence,
                complexity, strategy, traceBuilder, stopwatch);
        }

        /// <summary>判断是否为问候语</summary>
        internal bool IsGreeting(string message, ServiceDeskData config)
        {
            if (string.IsNullOrEmpty(message)) return false;
            var patterns = config?.GreetingPatterns ?? DefaultGreetingPatterns;
            return patterns.Any(p => message.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>判断是否为闲聊（感谢/告别等）</summary>
        internal bool IsSmallTalk(string message, ServiceDeskData config)
        {
            if (string.IsNullOrEmpty(message)) return false;
            var patterns = config?.SimpleConversationPatterns ?? DefaultSmallTalkPatterns;
            return patterns.Any(p => message.Contains(p, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>基于 IntentRule 检测用户意图</summary>
        internal IntentDetectionResult DetectIntent(string message, List<IntentRule> intentRules)
        {
            if (string.IsNullOrEmpty(message) || intentRules == null || intentRules.Count == 0)
                return new IntentDetectionResult();

            var matchedIntents = new List<(IntentRule rule, double score)>();

            foreach (var rule in intentRules)
            {
                double score = CalculateMatchScore(message, rule);
                if (score > 0.3)
                {
                    matchedIntents.Add((rule, score));
                }
            }

            var bestMatch = matchedIntents
                .OrderByDescending(x => x.rule.Priority)
                .ThenByDescending(x => x.score)
                .FirstOrDefault();

            if (bestMatch.rule == null)
                return new IntentDetectionResult();

            return new IntentDetectionResult
            {
                IntentName = bestMatch.rule.IntentName,
                Confidence = bestMatch.score,
                RequiredFields = bestMatch.rule.RequiredFields,
                RequiresConfirmation = bestMatch.rule.RequiresConfirmation
            };
        }

        /// <summary>
        /// 计算消息与意图规则的匹配分数
        /// 支持三种格式：简单匹配、AND(&amp;)逻辑、OR(|)逻辑
        /// </summary>
        internal double CalculateMatchScore(string message, IntentRule rule)
        {
            if (rule.Keywords == null || rule.Keywords.Count == 0)
                return 0;

            var msg = message.ToLower();
            int totalMatches = 0;
            int totalKeywords = rule.Keywords.Count;

            foreach (var keyword in rule.Keywords)
            {
                if (keyword.Contains('&'))
                {
                    // AND 逻辑: 所有部分都必须存在
                    var parts = keyword.Split('&');
                    if (parts.All(p => msg.Contains(p.Trim().ToLower())))
                        totalMatches++;
                }
                else if (keyword.Contains('|'))
                {
                    // OR 逻辑: 任一部分存在即可
                    var parts = keyword.Split('|');
                    if (parts.Any(p => msg.Contains(p.Trim().ToLower())))
                        totalMatches++;
                }
                else
                {
                    if (msg.Contains(keyword.ToLower()))
                        totalMatches++;
                }
            }

            return (double)totalMatches / totalKeywords;
        }

        /// <summary>评估消息复杂度</summary>
        internal MessageComplexity EvaluateComplexity(
            string message,
            IntentDetectionResult intentResult,
            MemoryContext memoryContext)
        {
            if (string.IsNullOrEmpty(message)) return MessageComplexity.Simple;

            int complexityScore = 0;

            // 因素1：消息长度
            if (message.Length > 200) complexityScore += 2;
            else if (message.Length > 100) complexityScore += 1;

            // 因素2：问号数量（多个问题）
            int questionCount = Regex.Matches(message, @"[?？]").Count;
            if (questionCount > 2) complexityScore += 2;
            else if (questionCount > 1) complexityScore += 1;

            // 因素3：是否需要信息收集
            if (intentResult?.RequiredFields?.Count > 0)
                complexityScore += 1;

            // 因素4：是否需要确认
            if (intentResult?.RequiresConfirmation == true)
                complexityScore += 1;

            // 因素5：是否涉及历史上下文
            if (memoryContext?.ShortTermMemory?.Count > 3)
                complexityScore += 1;

            if (complexityScore >= 5) return MessageComplexity.Complex;
            if (complexityScore >= 3) return MessageComplexity.Medium;
            return MessageComplexity.Simple;
        }

        /// <summary>根据分类结果决定处理策略</summary>
        internal ProcessingStrategy DetermineStrategy(
            IntentDetectionResult intentResult,
            MessageComplexity complexity,
            ServiceDeskData config)
        {
            // 策略1：直接回复（问候语、闲聊 — 在外层已处理）

            // 策略2：知识库检索（简单问答，意图置信度高）
            if (complexity == MessageComplexity.Simple && intentResult.Confidence >= 0.7)
                return ProcessingStrategy.KnowledgeRetrieval;

            // 策略3：RAG 增强（中等复杂度，或置信度中等）
            if (complexity == MessageComplexity.Medium ||
                (complexity == MessageComplexity.Simple && intentResult.Confidence >= 0.4))
                return ProcessingStrategy.RAGEnhanced;

            // 策略4：升级到 ClawAI（复杂问题，或置信度低）
            if (complexity == MessageComplexity.Complex || intentResult.Confidence < 0.4)
            {
                if (config?.EnableEscalation == true)
                    return ProcessingStrategy.EscalateToClawAI;
                return ProcessingStrategy.RAGEnhanced;
            }

            return ProcessingStrategy.RAGEnhanced;
        }

        private MessageType DetermineMessageType(IntentDetectionResult intentResult)
        {
            if (intentResult?.Confidence > 0 && intentResult.IntentName != "Unknown")
                return intentResult.RequiredFields?.Count > 0
                    ? MessageType.ComplexQuery
                    : MessageType.SimpleQA;
            return MessageType.Unknown;
        }

        private ClassificationResult BuildResult(
            MessageType type, string intent, double confidence,
            MessageComplexity complexity, ProcessingStrategy strategy,
            List<string> trace, Stopwatch stopwatch)
        {
            stopwatch.Stop();
            _logger.LogDebug($"[Classifier] Type={type}, Strategy={strategy}, Confidence={confidence:F2}, Elapsed={stopwatch.ElapsedMilliseconds}ms");
            return new ClassificationResult
            {
                Type = type,
                Intent = intent,
                Confidence = confidence,
                Complexity = complexity,
                Strategy = strategy,
                ReasoningTrace = string.Join(" → ", trace),
                ElapsedMs = stopwatch.ElapsedMilliseconds
            };
        }

        private static readonly List<string> DefaultGreetingPatterns = new()
        {
            "你好", "您好", "嗨", "hello", "hi", "在吗"
        };

        private static readonly List<string> DefaultSmallTalkPatterns = new()
        {
            "谢谢", "感谢", "再见", "拜拜", "好的", "知道了", "嗯"
        };
    }
}
