using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Node.Claw.Interfaces;
using ZSN.AI.Node.Claw.Utils;

namespace ZSN.AI.Node.Claw.Services
{
    /// <summary>
    /// 结果解析服务实现
    /// 支持多种数据提取方式：JSON、正则、LLM智能提取
    /// </summary>
    public class ResultParserService : IResultParserService
    {
        private readonly ILogger<ResultParserService> _logger;
        private readonly IChatService _chatService;

        public ResultParserService(
            ILogger<ResultParserService> logger,
            IChatService chatService)
        {
            _logger = logger;
            _chatService = chatService;
        }

        /// <summary>
        /// 从结果中提取 JSON 对象
        /// </summary>
        public async Task<object> ExtractJsonAsync(string result, string jsonPath = null)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $"提取 JSON - Path: {jsonPath ?? "root"}");

                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogWarning("[ResultParser] 输入结果为空，无法提取 JSON");
                    return null;
                }

                // 尝试提取 JSON 代码块
                string jsonContent;
                try
                {
                    jsonContent = ExtractJsonFromMarkdown(result);
                    LoggerHelper.LogDebug(_logger, ClawLogModules.RESULT_PARSER, " 提取到 JSON 内容，长度: {jsonContent.Length}");
                }
                catch (InvalidOperationException ex)
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, " 无法从结果中提取 JSON: {ex.Message}");
                    LoggerHelper.LogDebug(_logger, ClawLogModules.RESULT_PARSER, " 原始结果内容: {result.Substring(0, Math.Min(200, result.Length))}...");
                    return null;
                }
                
                // 解析 JSON
                var jToken = JToken.Parse(jsonContent);

                // 如果指定了路径，使用 JSONPath 查询
                if (!string.IsNullOrEmpty(jsonPath))
                {
                    var selected = jToken.SelectToken(jsonPath);
                    if (selected == null)
                    {
                        LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, " JSONPath '{jsonPath}' 未找到匹配的节点");
                    }
                    return selected?.ToObject<object>();
                }

                return jToken.ToObject<object>();
            }
            catch (JsonReaderException ex)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, " JSON 解析失败: {ex.Message}");
                LoggerHelper.LogDebug(_logger, ClawLogModules.RESULT_PARSER, " 尝试解析的内容: {result.Substring(0, Math.Min(200, result.Length))}...");
                return null;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, " JSON 提取失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 从结果中提取数组
        /// </summary>
        public async Task<List<object>> ExtractArrayAsync(string result, string arrayPath = null)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $"提取数组 - Path: {arrayPath ?? "root"}");

                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogWarning("[ResultParser] 输入结果为空，无法提取数组");
                    return new List<object>();
                }

                string jsonContent;
                try
                {
                    jsonContent = ExtractJsonFromMarkdown(result);
                }
                catch (InvalidOperationException ex)
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, " 无法从结果中提取 JSON: {ex.Message}");
                    return new List<object>();
                }

                var jToken = JToken.Parse(jsonContent);

                JArray jArray;
                if (!string.IsNullOrEmpty(arrayPath))
                {
                    var selected = jToken.SelectToken(arrayPath);
                    jArray = selected as JArray;
                }
                else
                {
                    jArray = jToken as JArray;
                }

                if (jArray == null)
                {
                    _logger.LogWarning("[ResultParser] 未找到数组");
                    return new List<object>();
                }

                var list = new List<object>();
                foreach (var item in jArray)
                {
                    list.Add(item.ToObject<object>());
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $" 提取到 {list.Count} 个数组元素");
                return list;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, $" 数组提取失败: {ex.Message}");
                return new List<object>();
            }
        }

        /// <summary>
        /// 使用正则表达式提取数据
        /// </summary>
        public async Task<List<string>> ExtractByRegexAsync(string result, string pattern, string groupName = null)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $" 正则提取 - Pattern: {pattern}");

                var matches = Regex.Matches(result, pattern, RegexOptions.Multiline);
                var results = new List<string>();

                foreach (Match match in matches)
                {
                    if (!string.IsNullOrEmpty(groupName) && match.Groups[groupName].Success)
                    {
                        results.Add(match.Groups[groupName].Value);
                    }
                    else
                    {
                        results.Add(match.Value);
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $" 提取到 {results.Count} 个匹配项");
                return results;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, $" 正则提取失败: {ex.Message}");
                return new List<string>();
            }
        }

        /// <summary>
        /// 使用 LLM 智能提取结构化数据
        /// </summary>
        public async Task<string> ExtractByLLMAsync(string result, string extractionPrompt, LargeModelConfig modelConfig)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, " LLM 智能提取");

                var systemPrompt = @"你是一个专业的数据提取助手。
你的任务是从给定的文本中提取结构化数据，并以 JSON 格式返回。

**重要规则**：
1. 只返回 JSON 格式的数据，不要包含任何解释文字
2. 确保 JSON 格式正确，可以被解析
3. 如果无法提取数据，返回空数组 []
4. 提取的数据应该完整且准确";

                var userPrompt = $@"## 原始文本
{result}

## 提取要求
{extractionPrompt}

请以 JSON 格式返回提取的数据。";

                // 构建 ChatHistory
                var chatHistory = new Microsoft.SemanticKernel.ChatCompletion.ChatHistory();
                chatHistory.AddSystemMessage(systemPrompt);
                chatHistory.AddUserMessage(userPrompt);

                // 调用 LLM
                var responseBuilder = new System.Text.StringBuilder();
                await foreach (var chunk in _chatService.SendChatAsync(
                    modelConfig,
                    chatHistory,
                    null,
                    "text",
                    false,
                    null,
                    System.Threading.CancellationToken.None))
                {
                    responseBuilder.Append(chunk);
                }

                var extractedData = responseBuilder.ToString();

                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $" LLM 提取完成，结果长度: {extractedData?.Length ?? 0}");
                return extractedData;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.RESULT_PARSER, $" LLM 提取失败: {ex.Message}");
                return "[]";
            }
        }

        /// <summary>
        /// 从结果中提取键值对
        /// </summary>
        public async Task<Dictionary<string, string>> ExtractKeyValuesAsync(string result, List<string> keys)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $" 提取键值对 - Keys: {string.Join(", ", keys)}");

                if (string.IsNullOrWhiteSpace(result))
                {
                    _logger.LogWarning("[ResultParser] 输入结果为空，无法提取键值对");
                    return new Dictionary<string, string>();
                }

                var kvPairs = new Dictionary<string, string>();
                
                string jsonContent;
                try
                {
                    jsonContent = ExtractJsonFromMarkdown(result);
                }
                catch (InvalidOperationException ex)
                {
                    LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, $" 无法从结果中提取 JSON: {ex.Message}");
                    return new Dictionary<string, string>();
                }

                var jObject = JObject.Parse(jsonContent);

                foreach (var key in keys)
                {
                    var value = jObject.SelectToken(key);
                    if (value != null)
                    {
                        kvPairs[key] = value.ToString();
                    }
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $" 提取到 {kvPairs.Count} 个键值对");
                return kvPairs;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogWarning(_logger, ClawLogModules.RESULT_PARSER, $" 键值对提取失败: {ex.Message}");
                return new Dictionary<string, string>();
            }
        }

        /// <summary>
        /// 将提取的数据转换为 Inputs 列表
        /// </summary>
        public async Task<List<Inputs>> ConvertToInputsAsync(object data, Dictionary<string, string> template)
        {
            try
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, " 转换为 Inputs");

                var inputs = new List<Inputs>();

                // 如果数据是数组，为每个元素创建一组 Inputs
                if (data is JArray jArray)
                {
                    foreach (var item in jArray)
                    {
                        var itemInputs = CreateInputsFromTemplate(item, template);
                        inputs.AddRange(itemInputs);
                    }
                }
                // 如果是单个对象
                else if (data is JObject jObject)
                {
                    var itemInputs = CreateInputsFromTemplate(jObject, template);
                    inputs.AddRange(itemInputs);
                }
                // 如果是字符串
                else if (data is string str)
                {
                    inputs.Add(new Inputs
                    {
                        varname = template.ContainsKey("varname") ? template["varname"] : "value",
                        value = str
                    });
                }

                LoggerHelper.LogInfo(_logger, ClawLogModules.RESULT_PARSER, $" 转换完成，生成 {inputs.Count} 个 Inputs");
                return inputs;
            }
            catch (Exception ex)
            {
                LoggerHelper.LogError(_logger, ClawLogModules.RESULT_PARSER, $" 转换失败: {ex.Message}");
                return new List<Inputs>();
            }
        }

        #region 私有辅助方法

        /// <summary>
        /// 从 Markdown 中提取 JSON 内容
        /// </summary>
        private string ExtractJsonFromMarkdown(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                throw new ArgumentException("输入文本为空");
            }

            // 尝试提取 ```json 代码块
            var jsonBlockMatch = Regex.Match(text, @"```json\s*([\s\S]*?)\s*```", RegexOptions.IgnoreCase);
            if (jsonBlockMatch.Success)
            {
                return jsonBlockMatch.Groups[1].Value.Trim();
            }

            // 尝试提取 ``` 代码块
            var codeBlockMatch = Regex.Match(text, @"```\s*([\s\S]*?)\s*```");
            if (codeBlockMatch.Success)
            {
                var content = codeBlockMatch.Groups[1].Value.Trim();
                if (IsLikelyJson(content))
                {
                    return content;
                }
            }

            // 尝试直接查找 JSON 对象或数组
            var jsonMatch = Regex.Match(text, @"(\{[\s\S]*\}|\[[\s\S]*\])");
            if (jsonMatch.Success)
            {
                var content = jsonMatch.Groups[1].Value.Trim();
                if (IsLikelyJson(content))
                {
                    return content;
                }
            }

            // 检查原文本是否像 JSON
            var trimmedText = text.Trim();
            if (IsLikelyJson(trimmedText))
            {
                return trimmedText;
            }

            // 如果都失败，抛出异常而不是返回原文本
            throw new InvalidOperationException($"无法从文本中提取有效的 JSON 内容。文本开头: {text.Substring(0, Math.Min(50, text.Length))}...");
        }

        /// <summary>
        /// 检查字符串是否可能是 JSON
        /// </summary>
        private bool IsLikelyJson(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return false;

            var trimmed = text.Trim();
            
            // JSON 必须以 { 或 [ 开头
            if (!trimmed.StartsWith("{") && !trimmed.StartsWith("["))
                return false;

            // JSON 必须以 } 或 ] 结尾
            if (!trimmed.EndsWith("}") && !trimmed.EndsWith("]"))
                return false;

            // 不能以注释符号开头
            if (trimmed.StartsWith("#") || trimmed.StartsWith("//"))
                return false;

            return true;
        }

        /// <summary>
        /// 根据模板创建 Inputs
        /// </summary>
        private List<Inputs> CreateInputsFromTemplate(JToken item, Dictionary<string, string> template)
        {
            var inputs = new List<Inputs>();

            foreach (var kvp in template)
            {
                var varname = kvp.Key;
                var valueKey = kvp.Value;

                // 如果 valueKey 是 JSONPath
                var value = item.SelectToken(valueKey);
                if (value != null)
                {
                    inputs.Add(new Inputs
                    {
                        varname = varname,
                        value = value.ToString()
                    });
                }
            }

            return inputs;
        }

        #endregion
    }
}
