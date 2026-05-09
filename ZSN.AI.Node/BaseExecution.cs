using Microsoft.Extensions.Logging;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Text;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Service.Helpers;
using ZSN.AI.Node.Utils;
using ZSN.Utils.Core.Helpers;
using ZSN.AI.Node.Claw.Utils;
using System.Text.RegularExpressions;

namespace ZSN.AI.Node
{
    public class BaseExecution
    {
        public readonly IChatService _chatService;
        public readonly IServiceProvider _provider;
        public readonly ILogger _logger;
        public readonly RedisStreamSync _streamSync;


        // 类型缓存,避免重复遍历程序集
        public static readonly Dictionary<string, System.Type> _typeCache = new Dictionary<string, System.Type>();
        public static readonly object _typeCacheLock = new object();

        public BaseExecution(IChatService chatService, IServiceProvider provider, ILogger<BaseExecution> logger) {
            _chatService = chatService;
            _provider = provider;
            _logger = logger;
            _streamSync = new RedisStreamSync(new RedisHelper());
        }
        public async Task<string> ReplacePromptValue(string prompt, List<Inputs> inputs, string nodeId, string SessionID, string AppID, string ProcessesID = null)
        {
            if (string.IsNullOrEmpty(prompt)) return prompt;

            // 构建缓存 (每次调用都查DB，为兼容旧路径)
            var cache = BuildPromptReplaceCache(inputs, nodeId, SessionID, AppID, ProcessesID);
            return await ReplacePromptValueWithCache(prompt, cache, SessionID, AppID);
        }

        /// <summary>
        /// P1优化: 使用预构建缓存的 ReplacePromptValue，避免重复 DB 查询
        /// </summary>
        public async Task<string> ReplacePromptValueCached(string prompt, PromptReplaceCache cache, string SessionID, string AppID, string ProcessesID = null)
        {
            if (string.IsNullOrEmpty(prompt)) return prompt;
            return await ReplacePromptValueWithCache(prompt, cache, SessionID, AppID);
        }

        /// <summary>
        /// P1优化: 预构建替换缓存 (一次DB查询，多次复用)
        /// </summary>
        public PromptReplaceCache BuildPromptReplaceCache(List<Inputs> inputs, string nodeId, string SessionID, string AppID, string ProcessesID = null)
        {
            var cache = new PromptReplaceCache();

            // 1) 收集并查询历史输出
            var noThisNodeInputs = inputs
                .Where(i => !string.IsNullOrEmpty(i.sourceId) && !i.sourceId.StartsWith(nodeId))
                .Select(i =>
                {
                    var (baseSourceId, _) = TaskInfoBussiness.ParseSourceId(i.sourceId);
                    i.id = baseSourceId.Split('_')[0];
                    return i;
                })
                .ToList();

            if (noThisNodeInputs.Count > 0)
            {
                noThisNodeInputs = noThisNodeInputs
                    .Where(i => !string.IsNullOrEmpty(i.id))
                    .GroupBy(i => i.id)
                    .Select(g => g.First())
                    .ToList();

                List<string> nodeIDList = noThisNodeInputs.Select(i => i.id).ToList();

                try
                {
                    List<WorkflowNodeExecutionRecordInfo> records = !string.IsNullOrEmpty(ProcessesID)
                        ? WorkflowNodeExecutionRecordInfoBussiness.GetListByNodeId(SessionID, nodeIDList, ProcessesID)
                        : WorkflowNodeExecutionRecordInfoBussiness.GetListByNodeId(SessionID, nodeIDList);
                    foreach (var record in records)
                    {
                        if (record.Outputs == null) continue;
                        List<Output> outputs = null;
                        try { outputs = JsonConvert.DeserializeObject<List<Output>>(record.Outputs.ToString()); }
                        catch { outputs = null; }
                        if (outputs == null) continue;
                        foreach (var output in outputs)
                        {
                            if (string.IsNullOrEmpty(output?.sourceId)) continue;
                            cache.OutputsDict[output.sourceId] = output.value ?? string.Empty;
                        }
                    }
                }
                catch { }
            }

            // 2) 收集输入字典
            foreach (var input in inputs ?? new List<Inputs>())
            {
                if (!string.IsNullOrEmpty(input.sourceId))
                    cache.InputSourceDict[input.sourceId] = input.value ?? string.Empty;
                if (!string.IsNullOrEmpty(input.varname))
                    cache.InputVarDict[input.varname] = input.value ?? string.Empty;
            }

            return cache;
        }

        /// <summary>
        /// 内部实现: 使用缓存执行替换
        /// </summary>
        private async Task<string> ReplacePromptValueWithCache(string prompt, PromptReplaceCache cache, string SessionID, string AppID)
        {
            if (string.IsNullOrEmpty(prompt)) return prompt;

            // 使用缓存中的字典 (已在 BuildPromptReplaceCache 中查询完毕)
            var outputsDict = cache.OutputsDict;
            var inputSourceDict = cache.InputSourceDict;
            var inputVarDict = cache.InputVarDict;

            // 3) 统一用正则解析并替换 {{ key }} 或 {{ key(jsonpath) }} 或 {{ key() }}
            // 优化：使用缓存的正则表达式，避免重复创建
            var keyWithPathRegex = ClawAIRegexPatterns.KeyWithPathExtractor;

            string Evaluator(System.Text.RegularExpressions.Match m)
            {
                var content = m.Groups[1].Value?.Trim();
                if (string.IsNullOrEmpty(content)) return m.Value; // 容错：内容为空

                var km = keyWithPathRegex.Match(content);
                if (!km.Success) return m.Value; // 容错：格式不匹配

                var baseKey = km.Groups[1].Value?.Trim();
                var pathExists = km.Groups[2].Success;
                var jsonPath = pathExists ? (km.Groups[2].Value ?? string.Empty).Trim() : string.Empty;

                if (string.IsNullOrEmpty(baseKey)) return m.Value;

                // ChatHistory 特殊处理: 跳过,留到最后单独替换
                if (baseKey.Equals("ChatHistory", StringComparison.OrdinalIgnoreCase))
                {
                    return m.Value; // 保留占位符,稍后处理
                }

                // 按优先级解析值：输出(sourceId) -> 输入(sourceId) -> 输入(varname)
                string rawValue = null;
                if (!string.IsNullOrEmpty(baseKey))
                {
                    var (normalizedBaseKey, _) = TaskInfoBussiness.ParseSourceId(baseKey);
                    if (outputsDict.TryGetValue(normalizedBaseKey, out var v1)) rawValue = v1;
                    else if (inputSourceDict.TryGetValue(baseKey, out var v2)) rawValue = v2;
                    else if (inputVarDict.TryGetValue(baseKey, out var v3)) rawValue = v3;
                }

                if (rawValue == null)
                {
                    return m.Value; // 找不到对应值，保留占位符
                }

                // 没有 JSONPath 或者括号为空 => 直接返回原值
                if (!pathExists || string.IsNullOrWhiteSpace(jsonPath))
                {
                    return rawValue;
                }

                // 有 JSONPath，尝试解析 rawValue 为 JSON 并提取
                try
                {
                    JToken token;
                    try
                    {
                        token = JToken.Parse(rawValue);
                    }
                    catch
                    {
                        // 非 JSON 文本，无法进行 JSONPath 提取，返回原值
                        return rawValue;
                    }

                    var tokens = token.SelectTokens(jsonPath, errorWhenNoMatch: false)?.ToList();
                    if (tokens != null && tokens.Count > 0)
                    {
                        var parts = new List<string>(tokens.Count);
                        foreach (var t in tokens)
                        {
                            if (t.Type == JTokenType.String) parts.Add(t.ToString());
                            else parts.Add(t.ToString(Formatting.None));
                        }
                        return string.Join("\n", parts);
                    }
                    // 未命中路径，回退为原值
                    return rawValue;
                }
                catch
                {
                    // 任意异常容错：返回原值
                    return rawValue;
                }
            }

            // 第一阶段: 替换所有非 ChatHistory 的占位符
            var result = ClawAIRegexPatterns.PlaceholderExtractor.Replace(prompt, new MatchEvaluator(Evaluator));

            // 第二阶段: 检查是否存在 ChatHistory 占位符,只有存在时才查询数据库并替换
            if (result.Contains("{{ChatHistory", StringComparison.OrdinalIgnoreCase))
            {
                ChatHistory history = new ChatHistory();
                List<AppChatLogInfo> appChatLogs = AppChatLogInfoBussiness.GetListBySessionID(AppID, SessionID);
                history = await _chatService.GetChatHistory(appChatLogs, history);

                // 单独处理 ChatHistory 占位符,避免其内容中的占位符被二次替换
                if (history != null && history.Count > 0)
                {
                    result = ClawAIRegexPatterns.PlaceholderExtractor.Replace(result, m =>
                    {
                        var content = m.Groups[1].Value?.Trim();
                        if (string.IsNullOrEmpty(content)) return m.Value;

                        var km = keyWithPathRegex.Match(content);
                        if (!km.Success) return m.Value;

                        var baseKey = km.Groups[1].Value?.Trim();
                        if (!baseKey.Equals("ChatHistory", StringComparison.OrdinalIgnoreCase))
                        {
                            return m.Value; // 不是 ChatHistory,保留
                        }

                        var pathExists = km.Groups[2].Success;
                        var jsonPath = pathExists ? (km.Groups[2].Value ?? string.Empty).Trim() : string.Empty;

                        try
                        {
                            // 如果有参数,解析为数字(取最近N条)
                            if (pathExists && !string.IsNullOrWhiteSpace(jsonPath))
                            {
                                if (int.TryParse(jsonPath, out int takeCount) && takeCount > 0)
                                {
                                    // 取最近N条记录
                                    var recentMessages = history.Skip(Math.Max(0, history.Count - takeCount)).ToList();
                                    return string.Join("\n", recentMessages.Select(x => $"{x.Role}: {x.Content}"));
                                }
                                else
                                {
                                    // 参数格式错误,返回占位符
                                    return m.Value;
                                }
                            }
                            else
                            {
                                // 无参数,返回全部历史记录
                                return string.Join("\n", history.Select(x => $"{x.Role}: {x.Content}"));
                            }
                        }
                        catch
                        {
                            // 异常容错:返回占位符
                            return m.Value;
                        }
                    });
                }
            }

            return result;
        }
    }

    /// <summary>
    /// P1优化: Prompt替换缓存 (一次DB查询，多次复用)
    /// </summary>
    public class PromptReplaceCache
    {
        public Dictionary<string, string> OutputsDict { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> InputSourceDict { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        public Dictionary<string, string> InputVarDict { get; } = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
    }
}
