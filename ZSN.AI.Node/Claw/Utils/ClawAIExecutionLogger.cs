using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using ZSN.AI.Entity;
using ZSN.AI.Entity.ClawAI;

namespace ZSN.AI.Node.Claw.Utils
{
    /// <summary>
    /// ClawAI 执行过程日志记录器。
    /// 每个任务生成独立日志文件，按阶段分段记录，便于追溯和分析。
    /// </summary>
    public class ClawAIExecutionLogger : IDisposable
    {
        private readonly string _logFilePath;
        private readonly string _taskID;
        private readonly StringBuilder _buffer = new StringBuilder();
        private bool _disposed;

        public ClawAIExecutionLogger(string taskID, string baseLogDir = "logs/clawai", int retentionDays = 1)
        {
            _taskID = taskID ?? "unknown";

            var dateDir = Path.Combine(baseLogDir, DateTime.Now.ToString("yyyy-MM-dd"));
            Directory.CreateDirectory(dateDir);

            var safeTaskID = string.Join("_", _taskID.Split(Path.GetInvalidFileNameChars()));
            _logFilePath = Path.Combine(dateDir, $"{safeTaskID}.log");

            // 清理过期日志（默认保留1天）
            CleanupExpiredLogs(baseLogDir, retentionDays);

            WriteHeader();
        }

        /// <summary>
        /// 清理过期的日志目录
        /// </summary>
        /// <param name="baseLogDir">日志根目录</param>
        /// <param name="retentionDays">保留天数</param>
        private void CleanupExpiredLogs(string baseLogDir, int retentionDays)
        {
            try
            {
                if (!Directory.Exists(baseLogDir))
                    return;

                var cutoffDate = DateTime.Now.Date.AddDays(-retentionDays);
                var directories = Directory.GetDirectories(baseLogDir);

                foreach (var dir in directories)
                {
                    var dirName = Path.GetFileName(dir);
                    
                    // 尝试解析目录名为日期（格式：yyyy-MM-dd）
                    if (DateTime.TryParseExact(dirName, "yyyy-MM-dd", 
                        System.Globalization.CultureInfo.InvariantCulture,
                        System.Globalization.DateTimeStyles.None, out DateTime dirDate))
                    {
                        // 如果目录日期早于截止日期，则删除
                        if (dirDate < cutoffDate)
                        {
                            try
                            {
                                Directory.Delete(dir, true);
                            }
                            catch (Exception ex)
                            {
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // 清理失败不影响主流程，只记录错误
            }
        }

        private void WriteHeader()
        {
            _buffer.AppendLine("================================================================");
            _buffer.AppendLine($"ClawAI 执行日志");
            _buffer.AppendLine($"TaskID: {_taskID}");
            _buffer.AppendLine($"时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            _buffer.AppendLine("================================================================");
            Flush();
        }

        /// <summary>
        /// 记录输入阶段：用户输入、系统提示词、可用WorkFlow
        /// </summary>
        public void LogInput(string userInput, string systemPrompt, List<WorkflowConfigInfo> availableWorkflows)
        {
            _buffer.AppendLine();
            _buffer.AppendLine("[1. 输入]");
            _buffer.AppendLine($"用户输入:");
            _buffer.AppendLine(Indent(userInput ?? ""));
            _buffer.AppendLine();

            _buffer.AppendLine($"系统提示词:");
            _buffer.AppendLine(Indent(systemPrompt ?? ""));
            _buffer.AppendLine();

            if (availableWorkflows != null && availableWorkflows.Count > 0)
            {
                _buffer.AppendLine($"可用 WorkFlow ({availableWorkflows.Count} 个):");
                foreach (var wf in availableWorkflows)
                {
                    _buffer.AppendLine($"  - {wf.workflowId}: {wf.name} | {wf.description}");
                }
            }

            Flush();
        }

        /// <summary>
        /// 记录规划阶段：提示词模板、LLM原始响应、校验结果、最终规划JSON
        /// </summary>
        public void LogPlanning(
            string promptTemplate,
            string llmRawResponse,
            string extractedJson,
            List<string> validationErrors,
            string finalPlanJson)
        {
            _buffer.AppendLine();
            _buffer.AppendLine("[2. 规划]");
            _buffer.AppendLine();

            _buffer.AppendLine("规划提示词模板:");
            _buffer.AppendLine(Indent(promptTemplate ?? ""));
            _buffer.AppendLine();

            _buffer.AppendLine($"LLM 原始响应 ({(llmRawResponse?.Length ?? 0)}字符):");
            _buffer.AppendLine(Indent(llmRawResponse ?? ""));
            _buffer.AppendLine();

            if (!string.IsNullOrEmpty(extractedJson))
            {
                _buffer.AppendLine("提取的 JSON:");
                _buffer.AppendLine(Indent(extractedJson));
                _buffer.AppendLine();
            }

            if (validationErrors != null && validationErrors.Count > 0)
            {
                _buffer.AppendLine("规划校验结果:");
                foreach (var err in validationErrors)
                {
                    _buffer.AppendLine($"  ✗ {err}");
                }
            }
            else
            {
                _buffer.AppendLine("规划校验结果: ✓ 通过");
            }
            _buffer.AppendLine();

            _buffer.AppendLine("最终任务规划 JSON:");
            _buffer.AppendLine(Indent(finalPlanJson ?? ""));

            Flush();
        }

        /// <summary>
        /// 记录单步执行
        /// </summary>
        public void LogStepExecution(
            TaskStep step,
            int? loopIndex,
            int? loopTotal,
            List<Inputs> inputs,
            string result,
            double durationSeconds)
        {
            _buffer.AppendLine();
            var loopInfo = step.LoopCount > 1 ? $" (循环 {loopIndex ?? 0}/{step.LoopCount})" : "";
            _buffer.AppendLine($"--- 步骤 {step.StepIndex}: {step.StepDescription}{loopInfo} ---");
            _buffer.AppendLine($"  StepID: {step.StepID}");
            _buffer.AppendLine($"  类型: {step.StepType}");
            _buffer.AppendLine($"  状态: {(step.StepStatus == StepStatus.Completed ? "✓ 完成" : step.StepStatus == StepStatus.Failed ? "✗ 失败" : step.StepStatus.ToString())}");
            _buffer.AppendLine($"  WorkFlow: [{string.Join(", ", step.AssignedWorkflowIds ?? new List<string>())}]");
            _buffer.AppendLine($"  依赖步骤: [{string.Join(", ", step.DependsOnStepIds ?? new List<string>())}]");
            _buffer.AppendLine($"  LoopCount: {step.LoopCount}");

            // StepInputs（原始占位符格式）
            if (step.StepInputs != null && step.StepInputs.Count > 0)
            {
                _buffer.AppendLine($"  StepInputs（原始占位符）:");
                foreach (var inp in step.StepInputs)
                {
                    _buffer.AppendLine($"    {inp.varname} = {inp.value ?? ""}");
                }
            }

            // 解析后的输入参数（传给 WorkFlow 的实际值）
            if (inputs != null && inputs.Count > 0)
            {
                _buffer.AppendLine($"  解析后输入参数（传给 WorkFlow）:");
                foreach (var inp in inputs)
                {
                    var value = inp.value ?? "";
                    // 对于超长内容，输出前 2000 字符 + 总长度
                    if (value.Length > 2000)
                    {
                        _buffer.AppendLine($"    {inp.varname} ({value.Length}字符) =");
                        _buffer.AppendLine(Indent(value.Substring(0, 2000)));
                        _buffer.AppendLine($"    ...(截断，共 {value.Length} 字符)");
                    }
                    else
                    {
                        _buffer.AppendLine($"    {inp.varname} ({value.Length}字符) =");
                        _buffer.AppendLine(Indent(value));
                    }
                }
            }

            _buffer.AppendLine($"  执行结果 ({result?.Length ?? 0}字符, {durationSeconds:F1}s):");
            // 结果截断到 5000 字符
            if (result != null && result.Length > 5000)
            {
                _buffer.AppendLine(Indent(result.Substring(0, 5000)));
                _buffer.AppendLine($"  ...(截断，共 {result.Length} 字符)");
            }
            else
            {
                _buffer.AppendLine(Indent(result ?? ""));
            }

            if (loopIndex.HasValue && loopIndex.Value == (loopTotal ?? 1))
            {
                _buffer.AppendLine($"  循环执行完成: {loopTotal} 次");
            }

            Flush();
        }

        /// <summary>
        /// 记录反思阶段
        /// </summary>
        public void LogReflection(
            string reflectionPrompt,
            ReflectionResult result,
            string goal)
        {
            _buffer.AppendLine();
            _buffer.AppendLine("[4. 反思]");

            if (!string.IsNullOrEmpty(goal))
            {
                _buffer.AppendLine($"任务目标: {goal}");
            }

            _buffer.AppendLine();
            _buffer.AppendLine("反思提示词:");
            _buffer.AppendLine(Indent(reflectionPrompt ?? ""));
            _buffer.AppendLine();

            _buffer.AppendLine("反思结果:");
            _buffer.AppendLine($"  完成度: {result?.CompletenessScore ?? 0}%");
            _buffer.AppendLine($"  质量: {result?.OverallQuality ?? 0}%");
            _buffer.AppendLine($"  行动: {result?.Action}");
            _buffer.AppendLine($"  理由: {result?.Reasoning ?? ""}");

            Flush();
        }

        /// <summary>
        /// 记录最终结果
        /// </summary>
        public void LogFinalResult(string finalAnswer, double totalDurationSeconds, TaskPlanning planning)
        {
            _buffer.AppendLine();
            _buffer.AppendLine("[5. 最终结果]");

            if (planning != null)
            {
                var completedCount = planning.Steps?.Count(s => s.StepStatus == StepStatus.Completed) ?? 0;
                var failedCount = planning.Steps?.Count(s => s.StepStatus == StepStatus.Failed) ?? 0;
                _buffer.AppendLine($"总步骤: {planning.TotalSteps} (完成: {completedCount}, 失败: {failedCount})");
                if (!string.IsNullOrEmpty(planning.Goal))
                {
                    _buffer.AppendLine($"目标: {planning.Goal}");
                }
            }

            _buffer.AppendLine($"总耗时: {totalDurationSeconds:F1}s");
            _buffer.AppendLine();

            _buffer.AppendLine("最终答案:");
            _buffer.AppendLine(Indent(finalAnswer ?? ""));

            _buffer.AppendLine("================================================================");
            Flush();
        }

        /// <summary>
        /// 记录通用日志行
        /// </summary>
        public void LogRaw(string message)
        {
            _buffer.AppendLine(message);
            Flush();
        }

        private void Flush()
        {
            try
            {
                File.AppendAllText(_logFilePath, _buffer.ToString());
                _buffer.Clear();
            }
            catch
            {
                // 日志写入失败不影响主流程
            }
        }

        private static string Indent(string text, int spaces = 2)
        {
            if (string.IsNullOrEmpty(text)) return "";
            var pad = new string(' ', spaces);
            var lines = text.Split('\n');
            return string.Join("\n", lines.Select(l => pad + l));
        }

        private static string Truncate(string text, int maxLength)
        {
            if (string.IsNullOrEmpty(text)) return "";
            if (text.Length <= maxLength) return text;
            return text.Substring(0, maxLength) + $"\n  ... (截断，共 {text.Length} 字符)";
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                Flush();
                _disposed = true;
            }
        }
    }
}
