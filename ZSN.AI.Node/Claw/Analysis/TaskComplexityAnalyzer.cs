using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSN.AI.Entity.ClawAI;
using ZSN.AI.Node.Claw.Configuration;
using ZSN.AI.Node.Claw.Utils;
using ZSN.AI.Node.Claw.Utils;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ZSN.AI.Node.Claw.Analysis
{
    /// <summary>
    /// 任务复杂度分析器 - 负责分析任务的复杂程度
    /// </summary>
    public class TaskComplexityAnalyzer
    {
        private readonly ClawAIOptions _options;
        private readonly ILogger _logger;

        public TaskComplexityAnalyzer(IOptions<ClawAIOptions> options, ILogger logger)
        {
            _options = options?.Value ?? new ClawAIOptions();
            _logger = logger;
        }

        /// <summary>
        /// 分析任务复杂度
        /// </summary>
        public TaskComplexity Analyze(string task, List<WorkflowConfigInfo> availableWorkflows)
        {
            if (string.IsNullOrWhiteSpace(task))
            {
                return TaskComplexity.Simple;
            }

            // 1. 检查任务长度
            int taskLength = task.Length;
            bool isShortTask = taskLength <= _options.TaskComplexity.SimpleTaskMaxLength;
            bool isLongTask = taskLength >= _options.TaskComplexity.ComplexTaskMinLength;

            // 2. 检查是否为知识查询
            bool isKnowledgeQuery = IsKnowledgeQuery(task);

            // 3. 检查是否为简单操作任务
            bool isSimpleOperation = IsSimpleOperation(task);

            // 4. 检查是否为复杂任务（包含多个连接词）
            bool isComplexByConnectors = IsComplexByConnectors(task);

            // 5. 检查是否为纯分析任务
            bool isPureAnalysis = IsPureAnalysis(task);

            // 决策逻辑
            if (isPureAnalysis)
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_COMPLEXITY, $"纯分析任务: {task}");
                return TaskComplexity.Simple; // 纯分析不创建步骤，直接返回
            }

            if (isShortTask || isKnowledgeQuery || isSimpleOperation)
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_COMPLEXITY, $"简单任务 - Length={taskLength}, IsKnowledgeQuery={isKnowledgeQuery}, IsSimpleOperation={isSimpleOperation}");
                return TaskComplexity.Simple;
            }

            if (isLongTask || isComplexByConnectors)
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_COMPLEXITY, $"复杂任务 - Length={taskLength}, IsComplexByConnectors={isComplexByConnectors}");
                return TaskComplexity.Complex;
            }

            // 默认为中等复杂度
            LoggerHelper.LogInfo(_logger, ClawLogModules.TASK_COMPLEXITY, "中等复杂度任务");
            return TaskComplexity.Medium;
        }

        /// <summary>
        /// 获取动态最大步骤数
        /// </summary>
        public int GetDynamicMaxSteps(TaskComplexity complexity, int configMaxSteps)
        {
            return complexity switch
            {
                TaskComplexity.Simple => _options.Planning.SimpleTaskMaxSteps,
                TaskComplexity.Medium => _options.Planning.MediumTaskMaxSteps,
                TaskComplexity.Complex => configMaxSteps,
                _ => configMaxSteps
            };
        }

        /// <summary>
        /// 检查是否为知识查询
        /// </summary>
        private bool IsKnowledgeQuery(string task)
        {
            if (string.IsNullOrWhiteSpace(task)) return false;

            var taskLower = task.ToLower();
            return _options.Planning.KnowledgeQueryPatterns.Any(pattern =>
                taskLower.Contains(pattern.ToLower()));
        }

        /// <summary>
        /// 检查是否为简单操作任务
        /// </summary>
        private bool IsSimpleOperation(string task)
        {
            if (string.IsNullOrWhiteSpace(task)) return false;

            var taskLower = task.ToLower();
            return _options.Planning.SimpleTaskPatterns.Any(pattern =>
                taskLower.Contains(pattern.ToLower()));
        }

        /// <summary>
        /// 检查是否包含多个连接词（复杂任务特征）
        /// </summary>
        private bool IsComplexByConnectors(string task)
        {
            if (string.IsNullOrWhiteSpace(task)) return false;

            int connectorCount = 0;
            foreach (var connector in _options.TaskComplexity.TaskConnectors)
            {
                if (task.Contains(connector))
                {
                    connectorCount++;
                }
            }

            return connectorCount >= 2; // 包含2个或以上连接词视为复杂
        }

        /// <summary>
        /// 检查是否为纯分析任务
        /// </summary>
        private bool IsPureAnalysis(string task)
        {
            if (string.IsNullOrWhiteSpace(task)) return false;

            var taskLower = task.ToLower();
            return _options.Planning.PureAnalysisPatterns.Any(pattern =>
                taskLower.Contains(pattern.ToLower()));
        }
    }

    /// <summary>
    /// 任务复杂度枚举
    /// </summary>
    public enum TaskComplexity
    {
        Simple,   // 简单任务
        Medium,   // 中等复杂度
        Complex   // 复杂任务
    }
}
