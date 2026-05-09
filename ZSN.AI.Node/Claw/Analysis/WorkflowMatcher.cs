using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ZSN.AI.Entity;
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
    /// WorkFlow匹配器 - 负责将任务步骤与可用的WorkFlow进行匹配
    /// </summary>
    public class WorkflowMatcher
    {
        private readonly ClawAIOptions _options;
        private readonly TextSimilarityCalculator _similarityCalculator;
        private readonly ILogger _logger;

        public WorkflowMatcher(
            IOptions<ClawAIOptions> options,
            TextSimilarityCalculator similarityCalculator,
            ILogger logger)
        {
            _options = options?.Value ?? new ClawAIOptions();
            _similarityCalculator = similarityCalculator;
            _logger = logger;
        }

        /// <summary>
        /// 尝试为步骤匹配合适的WorkFlow
        /// </summary>
        public List<string> TryMatchWorkflowsForStep(
            TaskStep step,
            List<WorkflowConfigInfo> availableWorkflows)
        {
            if (step == null || availableWorkflows == null || availableWorkflows.Count == 0)
            {
                return new List<string>();
            }

            var matchedWorkflows = new List<string>();
            var stepKeywords = _similarityCalculator.GetOrExtractKeywords(step.StepDescription);

            foreach (var workflow in availableWorkflows)
            {
                // 计算匹配分数
                double matchScore = _similarityCalculator.CalculateWorkflowMatchScore(stepKeywords, workflow);

                // 如果匹配分数超过阈值，则认为匹配
                if (matchScore >= _options.SimilarityThresholds.WorkflowMatch)
                {
                    matchedWorkflows.Add(workflow.workflowId);
                    LoggerHelper.LogDebug(_logger, ClawLogModules.WORKFLOW_MATCHER, $"步骤 '{step.StepDescription}' 匹配到 WorkFlow: {workflow.name} (分数: {matchScore:F2})");
                }
            }

            // 如果没有匹配到，检查步骤类型关键字
            if (matchedWorkflows.Count == 0)
            {
                matchedWorkflows = MatchByStepType(step, availableWorkflows);
            }

            return matchedWorkflows;
        }

        /// <summary>
        /// 根据步骤类型匹配WorkFlow
        /// </summary>
        private List<string> MatchByStepType(
            TaskStep step,
            List<WorkflowConfigInfo> availableWorkflows)
        {
            var matchedWorkflows = new List<string>();

            // 根据步骤类型的关键字进行匹配
            string stepTypeKeyword = GetStepTypeKeyword(step.StepType);
            if (string.IsNullOrEmpty(stepTypeKeyword))
            {
                return matchedWorkflows;
            }

            var stepKeywords = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                stepTypeKeyword
            };

            foreach (var workflow in availableWorkflows)
            {
                double matchScore = _similarityCalculator.CalculateWorkflowMatchScore(stepKeywords, workflow);

                if (matchScore >= _options.SimilarityThresholds.WorkflowMatch)
                {
                    matchedWorkflows.Add(workflow.workflowId);
                    LoggerHelper.LogDebug(_logger, ClawLogModules.WORKFLOW_MATCHER, $"步骤类型 '{step.StepType}' 匹配到 WorkFlow: {workflow.name} (分数: {matchScore:F2})");
                }
            }

            return matchedWorkflows;
        }

        /// <summary>
        /// 获取步骤类型的关键字
        /// </summary>
        private string GetStepTypeKeyword(StepType stepType)
        {
            return stepType switch
            {
                StepType.Query => "查询",
                StepType.Search => "搜索",
                StepType.Analysis => "分析",
                StepType.Generation => "生成",
                StepType.Processing => "处理",
                StepType.Validation => "验证",
                StepType.Optimization => "优化",
                _ => string.Empty
            };
        }

        /// <summary>
        /// 查找与任务最相关的WorkFlow（用于简化计划）
        /// </summary>
        public List<WorkflowConfigInfo> FindMostRelevantWorkflows(
            string task,
            List<WorkflowConfigInfo> availableWorkflows,
            int maxCount = 5)
        {
            if (string.IsNullOrEmpty(task) || availableWorkflows == null || availableWorkflows.Count == 0)
            {
                return new List<WorkflowConfigInfo>();
            }

            var taskKeywords = _similarityCalculator.GetOrExtractKeywords(task);

            // 计算每个WorkFlow的匹配分数
            var workflowScores = availableWorkflows
                .Select(wf => new
                {
                    Workflow = wf,
                    Score = _similarityCalculator.CalculateWorkflowMatchScore(taskKeywords, wf)
                })
                .OrderByDescending(x => x.Score)
                .Where(x => x.Score >= _options.SimilarityThresholds.WorkflowMatch)
                .Take(maxCount)
                .Select(x => x.Workflow)
                .ToList();

            if (workflowScores.Count > 0)
            {
                LoggerHelper.LogInfo(_logger, ClawLogModules.WORKFLOW_MATCHER, $"为任务找到 {workflowScores.Count} 个相关 WorkFlow");
            }

            return workflowScores;
        }

        /// <summary>
        /// 验证WorkFlow ID是否有效
        /// </summary>
        public bool IsValidWorkflowId(string workflowId)
        {
            if (string.IsNullOrEmpty(workflowId))
            {
                return false;
            }

            return ClawAIRegexPatterns.WorkflowIdValidator.IsMatch(workflowId);
        }

        /// <summary>
        /// 过滤无效的WorkFlow ID
        /// </summary>
        public List<string> FilterValidWorkflowIds(List<string> workflowIds)
        {
            if (workflowIds == null || workflowIds.Count == 0)
            {
                return new List<string>();
            }

            return workflowIds
                .Where(id => IsValidWorkflowId(id))
                .Distinct()
                .ToList();
        }
    }
}
