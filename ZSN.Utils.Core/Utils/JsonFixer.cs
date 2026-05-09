using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ZSN.Utils.Core.Utils
{
    // ========= Public API =========
    public class JsonFixResult
    {
        public bool IsSuccess { get; set; }
        public JObject Json { get; set; }
        public string FixedText { get; set; }
        public List<string> Warnings { get; set; } = new();
        public List<string> Errors { get; set; } = new();
        public IList<FixAction> ActionsTaken { get; set; } = new List<FixAction>();
        public JArray? JsonArray { get; set; }
        public JToken? Token { get; set; }
    }

    public interface IJsonFixer
    {
        JsonFixResult Fix(string rawText, JsonFixOptions? options = null);
        Task<JsonFixResult> FixAsync(string rawText, JsonFixOptions? options = null, CancellationToken ct = default);
    }

    public record JsonFixOptions(
        RecoveryMode Mode = RecoveryMode.Balanced,
        string? JsonSchema = null,
        ITextPreprocessor? Preprocessor = null,
        IJsonExtractor? Extractor = null,
        IJsonNormalizer? Normalizer = null,
        IJsonSchemaValidator? SchemaValidator = null,
        IObservationSink? Observation = null
    );

    public enum RecoveryMode
    {
        Strict,
        Balanced,
        Lenient
    }

    public enum FixActionType
    {
        Preprocess,
        Extract,
        Normalize,
        RemoveTrailingComma,
        QuoteBareProperty,
        QuoteBareValue,
        ConvertSingleToDoubleQuote,
        FillEmptyValue,
        SchemaCoerceType,
        SchemaAddMissing,
        SchemaRemoveAdditional,
        ParseSucceeded,
        ParseFailed
    }

    public record FixAction(
        FixActionType Type,
        string Description,
        string? Path = null,
        string? Before = null,
        string? After = null,
        double Confidence = 0.5
    );

    // ========= Extensibility Points =========
    public interface ITextPreprocessor
    {
        string Preprocess(string raw, IList<FixAction> actions, IList<string> warnings);
    }

    public interface IJsonExtractor
    {
        string ExtractCandidate(string text, IList<FixAction> actions, IList<string> warnings);
    }

    public interface IJsonNormalizer
    {
        string Normalize(string text, RecoveryMode mode, IList<FixAction> actions, IList<string> warnings);
    }

    public interface IJsonSchemaValidator
    {
        // returns (fixedText, actions/warnings appended)
        string ValidateAndRepair(string text, string schemaJson, RecoveryMode mode, IList<FixAction> actions, IList<string> warnings, IList<string> errors);
    }

    public interface IObservationSink
    {
        void OnStart(string stage, object? metadata = null);
        void OnStage(string stage, object? metadata = null);
        void OnError(string stage, Exception ex, object? metadata = null);
        void OnComplete(string stage, object? metadata = null);
    }

    // ========= Default Implementations =========
    public sealed class DefaultJsonFixer : IJsonFixer
    {
        private readonly ITextPreprocessor _pre;
        private readonly IJsonExtractor _ext;
        private readonly IJsonNormalizer _norm;
        private readonly IJsonSchemaValidator _schema;
        private readonly IObservationSink _obs;

        public DefaultJsonFixer(
            ITextPreprocessor? pre = null,
            IJsonExtractor? ext = null,
            IJsonNormalizer? norm = null,
            IJsonSchemaValidator? schema = null,
            IObservationSink? obs = null)
        {
            _pre = pre ?? new DefaultTextPreprocessor();
            _ext = ext ?? new DefaultJsonExtractor();
            _norm = norm ?? new DefaultJsonNormalizer();
            _schema = schema ?? new NoopJsonSchemaValidator();
            _obs = obs ?? new NoopObservationSink();
        }

        public JsonFixResult Fix(string rawText, JsonFixOptions? options = null)
        {
            options ??= new JsonFixOptions();
            var actions = new List<FixAction>();
            var warnings = new List<string>();
            var errors = new List<string>();
            _obs.OnStart("Fix", new { Mode = options.Mode });

            try
            {
                // 0) Try parse raw directly (return as-is if valid)
                if (TryParseToken(rawText ?? string.Empty, out JToken? tokenRaw))
                {
                    actions.Add(new FixAction(FixActionType.ParseSucceeded, "Parsed without repair (raw)"));
                    _obs.OnComplete("Fix", new { Success = true });
                    if (tokenRaw is JObject joRaw)
                    {
                        return new JsonFixResult { IsSuccess = true, Json = joRaw, JsonArray = null, Token = tokenRaw, FixedText = rawText ?? string.Empty, Warnings = warnings, Errors = errors, ActionsTaken = actions };
                    }
                    if (tokenRaw is JArray jaRaw)
                    {
                        return new JsonFixResult { IsSuccess = true, Json = new JObject(), JsonArray = jaRaw, Token = tokenRaw, FixedText = rawText ?? string.Empty, Warnings = warnings, Errors = errors, ActionsTaken = actions };
                    }
                }

                // 1) Preprocess
                _obs.OnStage("Preprocess");
                string pre = (options.Preprocessor ?? _pre).Preprocess(rawText ?? string.Empty, actions, warnings);

                // 2) Try parse fast
                if (TryParseToken(pre, out JToken? tokenFast))
                {
                    actions.Add(new FixAction(FixActionType.ParseSucceeded, "Parsed without repair"));
                    _obs.OnComplete("Fix", new { Success = true });
                    if (tokenFast is JObject joFast)
                    {
                        return new JsonFixResult { IsSuccess = true, Json = joFast, JsonArray = null, Token = tokenFast, FixedText = tokenFast.ToString(Formatting.None), Warnings = warnings, Errors = errors, ActionsTaken = actions };
                    }
                    if (tokenFast is JArray jaFast)
                    {
                        return new JsonFixResult { IsSuccess = true, Json = new JObject(), JsonArray = jaFast, Token = tokenFast, FixedText = tokenFast.ToString(Formatting.None), Warnings = warnings, Errors = errors, ActionsTaken = actions };
                    }
                    return new JsonFixResult { IsSuccess = false, Json = new JObject(), JsonArray = null, Token = tokenFast, FixedText = tokenFast.ToString(Formatting.None), Warnings = warnings, Errors = errors, ActionsTaken = actions };
                }
                actions.Add(new FixAction(FixActionType.ParseFailed, "Initial parse failed"));

                // 3) Extract candidate JSON
                _obs.OnStage("Extract");
                string extracted = (options.Extractor ?? _ext).ExtractCandidate(pre, actions, warnings);
                string work = string.IsNullOrWhiteSpace(extracted) ? pre : extracted;

                // 4) Normalize by mode
                _obs.OnStage("Normalize");
                work = (options.Normalizer ?? _norm).Normalize(work, options.Mode, actions, warnings);

                // 5) Try parse after normalize
                if (!TryParseToken(work, out JToken? token))
                {
                    // 6) Schema-driven (optional)
                    if (!string.IsNullOrWhiteSpace(options.JsonSchema))
                    {
                        _obs.OnStage("SchemaValidate");
                        work = (options.SchemaValidator ?? _schema).ValidateAndRepair(work, options.JsonSchema!, options.Mode, actions, warnings, errors);
                    }
                }

                if (TryParseToken(work, out JToken? tokenAfter))
                {
                    actions.Add(new FixAction(FixActionType.ParseSucceeded, "Parsed after repairs"));
                    _obs.OnComplete("Fix", new { Success = true });
                    if (tokenAfter is JObject joAfter)
                    {
                        return new JsonFixResult { IsSuccess = true, Json = joAfter, JsonArray = null, Token = tokenAfter, FixedText = tokenAfter.ToString(Formatting.None), Warnings = warnings, Errors = errors, ActionsTaken = actions };
                    }
                    if (tokenAfter is JArray jaAfter)
                    {
                        return new JsonFixResult { IsSuccess = true, Json = new JObject(), JsonArray = jaAfter, Token = tokenAfter, FixedText = tokenAfter.ToString(Formatting.None), Warnings = warnings, Errors = errors, ActionsTaken = actions };
                    }
                    return new JsonFixResult { IsSuccess = false, Json = new JObject(), JsonArray = null, Token = tokenAfter, FixedText = tokenAfter.ToString(Formatting.None), Warnings = warnings, Errors = errors, ActionsTaken = actions };
                }

                errors.Add("Unable to parse JSON after repair attempts.");
                _obs.OnComplete("Fix", new { Success = false });
                return new JsonFixResult { IsSuccess = false, Json = new JObject(), FixedText = work, Warnings = warnings, Errors = errors, ActionsTaken = actions };
            }
            catch (Exception ex)
            {
                errors.Add("Fatal error during Fix: " + ex.Message);
                _obs.OnError("Fix", ex);
                _obs.OnComplete("Fix", new { Success = false });
                return new JsonFixResult { IsSuccess = false, Json = new JObject(), FixedText = rawText ?? string.Empty, Warnings = warnings, Errors = errors, ActionsTaken = actions };
            }
        }

        public Task<JsonFixResult> FixAsync(string rawText, JsonFixOptions? options = null, CancellationToken ct = default)
        {
            // CPU-bound; preserve thread safety by using local state only.
            return Task.Run(() => Fix(rawText, options), ct);
        }

        private static bool TryParseObject(string text, out JObject? obj)
        {
            obj = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                var token = JToken.Parse(text);
                if (token is JObject jo)
                {
                    obj = jo;
                    return true;
                }
                return false;
            }
            catch
            {
                return false;
            }
        }

        private static bool TryParseToken(string text, out JToken? token)
        {
            token = null;
            if (string.IsNullOrWhiteSpace(text)) return false;
            try
            {
                token = JToken.Parse(text);
                return token is JObject || token is JArray;
            }
            catch
            {
                return false;
            }
        }
    }

    // ========= Default Strategies =========
    public sealed class DefaultTextPreprocessor : ITextPreprocessor
    {
        public string Preprocess(string raw, IList<FixAction> actions, IList<string> warnings)
        {
            string text = (raw ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Step 1: 尝试提取代码围栏块 (```json ... ``` 或 ``` ... ```)
            text = ExtractFromCodeFence(text, actions, warnings);

            // Step 2: 如果仍然包含```标记但提取失败,尝试更激进的清理
            if (text.Contains("```"))
            {
                text = AggressiveCodeFenceCleanup(text, actions, warnings);
            }

            // Step 3: 替换智能引号和特殊字符
            text = text
                .Replace('\u201C', '"').Replace('\u201D', '"')  // 中文双引号
                .Replace('\u2018', '\'').Replace('\u2019', '\'') // 中文单引号
                .Replace('\u2013', '-').Replace('\u2014', '-')   // 短横线、长横线
                .Replace('\u00A0', ' ');                          // 不间断空格

            // Step 4: 移除控制字符 (保留空格、制表符、换行符)
            var sb = new StringBuilder(text.Length);
            foreach (char c in text)
            {
                if (char.IsControl(c) && c != '\n' && c != '\r' && c != '\t') continue;
                sb.Append(c);
            }
            string cleaned = sb.ToString().Trim();
            
            if (!ReferenceEquals(cleaned, text) && cleaned != text)
                actions.Add(new FixAction(FixActionType.Preprocess, "Removed control characters"));

            return cleaned;
        }

        /// <summary>
        /// 从代码围栏中提取JSON内容
        /// 支持格式: ```json\n{...}\n```, ```\n{...}\n```, ``` json\n{...}\n```
        /// </summary>
        private string ExtractFromCodeFence(string text, IList<FixAction> actions, IList<string> warnings)
        {
            // 查找所有可能的代码块
            int searchStart = 0;
            while (searchStart < text.Length)
            {
                int fenceStart = text.IndexOf("```", searchStart, StringComparison.Ordinal);
                if (fenceStart < 0) break;

                int fenceEnd = text.IndexOf("```", fenceStart + 3, StringComparison.Ordinal);
                if (fenceEnd <= fenceStart)
                {
                    // 没有找到结束标记,尝试提取从```到字符串末尾
                    warnings.Add("Code fence not properly closed, attempting to extract content");
                    string remaining = text.Substring(fenceStart + 3).Trim();
                    string extracted = ExtractJsonFromFenceContent(remaining);
                    if (!string.IsNullOrWhiteSpace(extracted))
                    {
                        actions.Add(new FixAction(FixActionType.Preprocess, "Extracted from unclosed code fence"));
                        return extracted;
                    }
                    break;
                }

                // 提取围栏内的内容
                string inner = text.Substring(fenceStart + 3, fenceEnd - (fenceStart + 3));
                string extractedContent = ExtractJsonFromFenceContent(inner);
                
                if (!string.IsNullOrWhiteSpace(extractedContent))
                {
                    actions.Add(new FixAction(FixActionType.Preprocess, "Removed fenced code block"));
                    return extractedContent;
                }

                // 继续查找下一个代码块
                searchStart = fenceEnd + 3;
            }

            return text;
        }

        /// <summary>
        /// 从围栏内容中提取JSON
        /// 处理: json\n{...}, \njson\n{...}, json{...}, {... 等格式
        /// </summary>
        private string ExtractJsonFromFenceContent(string fenceContent)
        {
            if (string.IsNullOrWhiteSpace(fenceContent)) return string.Empty;

            string content = fenceContent.Trim();

            // 情况1: 以 "json" 开头 (不区分大小写,可能有空格)
            // 支持: json\n{...}, json {..}, json{...}
            if (content.StartsWith("json", StringComparison.OrdinalIgnoreCase))
            {
                // 跳过 "json" 和可能的空格
                int skipLength = 4; // "json".Length
                while (skipLength < content.Length && (content[skipLength] == ' ' || content[skipLength] == '\t'))
                {
                    skipLength++;
                }
                content = content.Substring(skipLength).Trim();
            }

            // 情况2: 检查是否以 { 或 [ 开头 (有效的JSON起始)
            if (content.StartsWith("{") || content.StartsWith("["))
            {
                return content;
            }

            // 情况3: 可能有其他前缀文本,尝试找到第一个 { 或 [
            int jsonStart = -1;
            int braceIndex = content.IndexOf('{');
            int bracketIndex = content.IndexOf('[');
            
            if (braceIndex >= 0 && bracketIndex >= 0)
            {
                jsonStart = Math.Min(braceIndex, bracketIndex);
            }
            else if (braceIndex >= 0)
            {
                jsonStart = braceIndex;
            }
            else if (bracketIndex >= 0)
            {
                jsonStart = bracketIndex;
            }

            if (jsonStart > 0)
            {
                return content.Substring(jsonStart).Trim();
            }

            return content;
        }

        /// <summary>
        /// 激进的代码围栏清理 - 当标准提取失败时使用
        /// 直接移除所有```标记
        /// </summary>
        private string AggressiveCodeFenceCleanup(string text, IList<FixAction> actions, IList<string> warnings)
        {
            warnings.Add("Standard code fence extraction failed, using aggressive cleanup");
            
            // 移除所有```标记及其后的语言标识
            string cleaned = Regex.Replace(text, @"```\s*\w*\s*", "", RegexOptions.IgnoreCase);
            
            // 再次清理可能残留的```
            cleaned = cleaned.Replace("```", "");
            
            // 尝试找到第一个有效的JSON起始位置
            int jsonStart = -1;
            int braceIndex = cleaned.IndexOf('{');
            int bracketIndex = cleaned.IndexOf('[');
            
            if (braceIndex >= 0 && bracketIndex >= 0)
            {
                jsonStart = Math.Min(braceIndex, bracketIndex);
            }
            else if (braceIndex >= 0)
            {
                jsonStart = braceIndex;
            }
            else if (bracketIndex >= 0)
            {
                jsonStart = bracketIndex;
            }

            if (jsonStart >= 0)
            {
                cleaned = cleaned.Substring(jsonStart);
                actions.Add(new FixAction(FixActionType.Preprocess, "Aggressively removed code fence markers"));
            }

            return cleaned.Trim();
        }
    }

    public sealed class DefaultJsonExtractor : IJsonExtractor
    {
        public string ExtractCandidate(string text, IList<FixAction> actions, IList<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            // Try object first
            int start = text.IndexOf('{');
            if (start >= 0 && TryExtractBalanced(text, start, '{', '}', out string obj))
            {
                actions.Add(new FixAction(FixActionType.Extract, "Extracted object candidate"));
                return obj;
            }

            // Then array
            start = text.IndexOf('[');
            if (start >= 0 && TryExtractBalanced(text, start, '[', ']', out string arr))
            {
                actions.Add(new FixAction(FixActionType.Extract, "Extracted array candidate"));
                return arr;
            }

            // Fallback: attempt longest balanced braces scanning
            string longest = string.Empty;
            for (int i = 0; i < text.Length; i++)
            {
                char ch = text[i];
                if (ch == '{' || ch == '[')
                {
                    char open = ch;
                    char close = (ch == '{') ? '}' : ']';
                    if (TryExtractBalanced(text, i, open, close, out string cand) && cand.Length > longest.Length)
                    {
                        longest = cand;
                    }
                }
            }
            if (!string.IsNullOrEmpty(longest))
            {
                actions.Add(new FixAction(FixActionType.Extract, "Extracted longest balanced candidate"));
                return longest;
            }

            warnings.Add("No JSON candidate found.");
            return string.Empty;
        }

        private static bool TryExtractBalanced(string text, int startIndex, char open, char close, out string result)
        {
            result = string.Empty;
            if (startIndex < 0 || startIndex >= text.Length || text[startIndex] != open) return false;

            int depth = 0;
            bool inString = false;
            bool escaped = false;

            for (int i = startIndex; i < text.Length; i++)
            {
                char c = text[i];
                if (inString)
                {
                    if (escaped) { escaped = false; }
                    else if (c == '\\') { escaped = true; }
                    else if (c == '"') { inString = false; }
                }
                else
                {
                    if (c == '"') inString = true;
                    else if (c == open) depth++;
                    else if (c == close)
                    {
                        depth--;
                        if (depth == 0)
                        {
                            result = text.Substring(startIndex, i - startIndex + 1).Trim();
                            return true;
                        }
                    }
                }
            }
            return false;
        }
    }

    public sealed class DefaultJsonNormalizer : IJsonNormalizer
    {
        // Regex cached
        private static readonly Regex BareProp = new(@"(?<=[{,]\s*)([A-Za-z_\p{L}][A-Za-z0-9_\p{L}]*)\s*(?=:)", RegexOptions.Compiled);
        private static readonly Regex EmptyValue = new(@":\s*(?=[,}])", RegexOptions.Compiled);
        private static readonly Regex BareWordValue = new(@"(:\s*)(?![\""\[\{\-\d]|true\b|false\b|null\b)([A-Za-z_\p{L}][A-Za-z0-9_\p{L}]*)\b(?=\s*[,}\]])", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TrailingComma = new(@",\s*([}\]])", RegexOptions.Compiled);
        private static readonly Regex SingleQuoted = new(@"'([^']*?)'", RegexOptions.Compiled);

        public string Normalize(string text, RecoveryMode mode, IList<FixAction> actions, IList<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(text)) return string.Empty;

            string normalized = text;
            string before;

            // Pass 1: remove trailing commas (safe)
            before = normalized;
            normalized = TrailingComma.Replace(normalized, "$1");
            if (!ReferenceEquals(before, normalized) && before != normalized)
                actions.Add(new FixAction(FixActionType.RemoveTrailingComma, "Removed trailing comma"));

            // Strict mode stops early with only safest changes
            if (mode == RecoveryMode.Strict)
            {
                return normalized.Trim();
            }

            // Pass 2: quote bare property names
            before = normalized;
            normalized = BareProp.Replace(normalized, m =>
            {
                string name = m.Groups[1].Value;
                return $"\"{name}\"";
            });
            if (!ReferenceEquals(before, normalized) && before != normalized)
                actions.Add(new FixAction(FixActionType.QuoteBareProperty, "Quoted bare property names"));

            // Pass 3: fill empty values
            before = normalized;
            normalized = EmptyValue.Replace(normalized, ": \"\"");
            if (!ReferenceEquals(before, normalized) && before != normalized)
                actions.Add(new FixAction(FixActionType.FillEmptyValue, "Filled empty values with \"\""));

            // Pass 4: convert single quotes to double quotes (lenient/balanced)
            if (mode != RecoveryMode.Strict)
            {
                before = normalized;
                normalized = SingleQuoted.Replace(normalized, m => $"\"{m.Groups[1].Value}\"");
                if (!ReferenceEquals(before, normalized) && before != normalized)
                    actions.Add(new FixAction(FixActionType.ConvertSingleToDoubleQuote, "Converted single to double quotes"));
            }

            // Pass 5: quote bareword values (balanced/lenient)
            if (mode != RecoveryMode.Strict)
            {
                before = normalized;
                normalized = BareWordValue.Replace(normalized, "$1\"$2\"");
                if (!ReferenceEquals(before, normalized) && before != normalized)
                    actions.Add(new FixAction(FixActionType.QuoteBareValue, "Quoted bareword values"));
            }

            return normalized.Trim();
        }
    }

    public sealed class NoopJsonSchemaValidator : IJsonSchemaValidator
    {
        public string ValidateAndRepair(string text, string schemaJson, RecoveryMode mode, IList<FixAction> actions, IList<string> warnings, IList<string> errors)
        {
            // Placeholder: only note that schema validation is not active.
            warnings.Add("Schema validation not enabled (NoopJsonSchemaValidator). Provide a real validator to enable schema-driven repairs.");
            return text;
        }
    }

    public sealed class NoopObservationSink : IObservationSink
    {
        public void OnStart(string stage, object? metadata = null) { }
        public void OnStage(string stage, object? metadata = null) { }
        public void OnError(string stage, Exception ex, object? metadata = null) { }
        public void OnComplete(string stage, object? metadata = null) { }
    }
}
