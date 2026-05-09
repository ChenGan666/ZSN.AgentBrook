using System.Text;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using ZSN.AI.Entity;
using ZSN.AI.Node.ServiceDesk.Interfaces;
using ZSN.AI.Node.ServiceDesk.Models;

namespace ZSN.AI.Node.ServiceDesk.Services
{
    /// <summary>
    /// 会话状态管理器 — 管理多轮对话状态转换和信息收集
    /// </summary>
    public class SessionStateManager : ISessionStateManager
    {
        private readonly ILogger<SessionStateManager> _logger;

        // 内存缓存（后续可替换为 Redis）
        private static readonly Dictionary<string, SessionStateContext> _stateCache = new();
        private static readonly object _cacheLock = new();

        public SessionStateManager(ILogger<SessionStateManager> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 获取或创建会话状态
        /// </summary>
        public async Task<SessionStateContext> GetOrCreateSessionStateAsync(
            string sessionId,
            string appId,
            string memberId)
        {
            lock (_cacheLock)
            {
                if (_stateCache.TryGetValue(sessionId, out var state))
                {
                    // 检查超时（默认5分钟）
                    if ((DateTime.Now - state.LastUpdateTime).TotalMinutes > 5)
                    {
                        _stateCache.Remove(sessionId);
                        _logger.LogInformation($"[SessionState] 会话状态超时，重置: {sessionId}");
                    }
                    else
                    {
                        return state;
                    }
                }
            }

            var newState = new SessionStateContext
            {
                SessionId = sessionId,
                AppId = appId,
                MemberId = memberId,
                CurrentState = SessionState.Idle,
                CollectedInfo = new Dictionary<string, string>(),
                MissingFields = new List<string>(),
                CreateTime = DateTime.Now,
                LastUpdateTime = DateTime.Now
            };

            await SaveSessionStateAsync(newState);
            return newState;
        }

        /// <summary>
        /// 转换会话状态
        /// </summary>
        public async Task<SessionStateContext> TransitionStateAsync(
            SessionStateContext currentState,
            SessionState targetState,
            string reason = null)
        {
            if (!IsValidTransition(currentState.CurrentState, targetState))
            {
                _logger.LogWarning(
                    $"[SessionState] 无效的状态转换: {currentState.CurrentState} → {targetState}");
                return currentState;
            }

            currentState.StateHistory ??= new List<StateTransition>();
            currentState.StateHistory.Add(new StateTransition
            {
                FromState = currentState.CurrentState,
                ToState = targetState,
                Reason = reason,
                Timestamp = DateTime.Now
            });

            var oldState = currentState.CurrentState;
            currentState.CurrentState = targetState;
            currentState.LastUpdateTime = DateTime.Now;

            await SaveSessionStateAsync(currentState);

            _logger.LogInformation(
                $"[SessionState] {currentState.SessionId}: {oldState} → {targetState} ({reason})");

            return currentState;
        }

        /// <summary>
        /// 收集用户信息
        /// </summary>
        public async Task<SessionStateContext> CollectInformationAsync(
            SessionStateContext state,
            string userMessage,
            IntentRule intentRule)
        {
            // 提取信息
            var extractedInfo = ExtractInformationFromMessage(userMessage, intentRule);

            foreach (var kvp in extractedInfo)
            {
                state.CollectedInfo[kvp.Key] = kvp.Value;
            }

            // 更新缺失字段
            state.MissingFields = intentRule.RequiredFields
                .Where(field => !state.CollectedInfo.ContainsKey(field))
                .ToList();

            // 判断是否收集完成
            if (state.MissingFields.Count == 0)
            {
                if (intentRule.RequiresConfirmation)
                {
                    await TransitionStateAsync(state, SessionState.WaitingForConfirmation, "信息收集完成，需要确认");
                }
                else
                {
                    await TransitionStateAsync(state, SessionState.Completed, "信息收集完成，无需确认");
                }
            }
            else
            {
                if (state.CurrentState != SessionState.InformationGathering)
                {
                    await TransitionStateAsync(state, SessionState.InformationGathering, "开始收集信息");
                }
                else
                {
                    await SaveSessionStateAsync(state);
                }
            }

            return state;
        }

        /// <summary>
        /// 生成缺失字段的提示消息
        /// </summary>
        public string GeneratePromptForMissingFields(SessionStateContext state)
        {
            if (state.MissingFields == null || state.MissingFields.Count == 0)
                return null;

            if (state.MissingFields.Count == 1)
            {
                return $"请提供{state.MissingFields[0]}。";
            }

            var sb = new StringBuilder();
            sb.Append("请提供以下信息：\n");
            for (int i = 0; i < state.MissingFields.Count; i++)
            {
                sb.AppendLine($"{i + 1}. {state.MissingFields[i]}");
            }
            return sb.ToString();
        }

        /// <summary>
        /// 保存会话状态
        /// </summary>
        public Task SaveSessionStateAsync(SessionStateContext state)
        {
            state.LastUpdateTime = DateTime.Now;
            lock (_cacheLock)
            {
                _stateCache[state.SessionId] = state;
            }
            return Task.CompletedTask;
        }

        /// <summary>验证状态转换是否合法</summary>
        private bool IsValidTransition(SessionState from, SessionState to)
        {
            return (from, to) switch
            {
                (SessionState.Idle, SessionState.InformationGathering) => true,
                (SessionState.Idle, SessionState.ProcessingRequest) => true,
                (SessionState.Idle, SessionState.Escalated) => true,
                (SessionState.InformationGathering, SessionState.ProcessingRequest) => true,
                (SessionState.InformationGathering, SessionState.WaitingForConfirmation) => true,
                (SessionState.InformationGathering, SessionState.Idle) => true,
                (SessionState.ProcessingRequest, SessionState.WaitingForConfirmation) => true,
                (SessionState.ProcessingRequest, SessionState.Completed) => true,
                (SessionState.WaitingForConfirmation, SessionState.Completed) => true,
                (SessionState.WaitingForConfirmation, SessionState.InformationGathering) => true,
                (SessionState.WaitingForConfirmation, SessionState.Idle) => true,
                (SessionState.Completed, SessionState.Idle) => true,
                (SessionState.Escalated, SessionState.Idle) => true,
                _ => false
            };
        }

        /// <summary>从消息中提取信息</summary>
        private Dictionary<string, string> ExtractInformationFromMessage(
            string message,
            IntentRule intentRule)
        {
            var result = new Dictionary<string, string>();

            if (intentRule.FieldExtractionPatterns == null || string.IsNullOrEmpty(message))
                return result;

            foreach (var pattern in intentRule.FieldExtractionPatterns)
            {
                try
                {
                    var match = Regex.Match(message, pattern.Value);
                    if (match.Success)
                    {
                        result[pattern.Key] = match.Groups.Count > 1 ? match.Groups[1].Value : match.Value;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, $"字段提取正则错误: {pattern.Key} - {pattern.Value}");
                }
            }

            // 兜底：描述性字段直接使用用户输入
            foreach (var field in intentRule.RequiredFields ?? new List<string>())
            {
                if (result.ContainsKey(field)) continue;
                if (IsDescriptiveField(field) && !IsShortCode(message))
                {
                    result[field] = message;
                }
            }

            return result;
        }

        private bool IsDescriptiveField(string fieldName)
        {
            return fieldName.Contains("原因") || fieldName.Contains("描述") ||
                   fieldName.Contains("说明") || fieldName.Contains("内容");
        }

        private bool IsShortCode(string message)
        {
            return Regex.IsMatch(message, @"^\s*[A-Z0-9\-]+\s*$") ||
                   Regex.IsMatch(message, @"^\s*\d{11}\s*$");
        }
    }
}
