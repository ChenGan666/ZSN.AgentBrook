using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity.Model;
using ZSN.Utils.Core.Extensions;
using System.Collections;
using System.Threading;

namespace ZSN.AI.Core.Service
{
    [ServiceDescription(typeof(IAgentSkillService), ServiceLifetime.Scoped)]
    public class AgentSkillService : IAgentSkillService
    {
        private const int LLMLogMarkId = 312;
        private readonly IKernelService _kernelService;
        private readonly IOperationLogService _logService;
        private class PlanModel
        {
            public string title { get; set; } = "Plan";
            public string createdAt { get; set; } = DateTime.Now.ToString("s");
            public string sessionId { get; set; }
            public string processesId { get; set; }
            public string skillDirectory { get; set; }
            public string status { get; set; } = "in_progress";
            public string originalPlanJson { get; set; }
            public List<PlanStep> Steps { get; set; } = new List<PlanStep>();
        }

        private class PlanStep
        {
            public string id { get; set; }
            public string name { get; set; }
            public RunSpec run { get; set; } = new RunSpec();
            public int tryCount { get; set; } = 0;
            public int maxTry { get; set; } = 1;
            public string status { get; set; } = "pending";
            public string startedAt { get; set; }
            public string finishedAt { get; set; }
            public int exitCode { get; set; }
            public string stdout { get; set; }
            public string stderr { get; set; }
            public ValidateSpec validate { get; set; } = new ValidateSpec();
            public string notes { get; set; }
        }

        private class RunSpec
        {
            public string relativePath { get; set; }
            public List<string> args { get; set; } = new List<string>();
            public Dictionary<string, string> env { get; set; } = new Dictionary<string, string>();
            public string workingDir { get; set; }
        }

        private class ValidateSpec
        {
            public string type { get; set; } = "basic";
            public string rule { get; set; } = "exitCode==0";
            public string prompt { get; set; }
        }
        
        public AgentSkillService(IKernelService kernelService, IOperationLogService logService)
        {
            _kernelService = kernelService;
            _logService = logService;
        }

        private string BuildTaskRoot(string skillRoot, string sessionId, string processesId)
        {
            string sid = string.IsNullOrEmpty(sessionId) ? "default" : sessionId;
            string pid = string.IsNullOrEmpty(processesId) ? "default" : processesId;
            return Path.Combine(skillRoot, "temp", $"{sid}_{pid}");
        }

        public async Task<AgentSkillResponse> PlanAsync(
            Skill skill,
            SkillsToolsOptions options,
            string prompt,
            List<AttachmentItem> attachments = null,
            IProgress<string> progress = null,
            LargeModelConfig modelConfig = null,
            CancellationToken ct = default)
        {
            var resp = new AgentSkillResponse();
            try
            {
                if (skill == null)
                {
                    resp.Logs.Add("Skill 为空");
                    resp.Output = "Skill 未提供";
                    return resp;
                }
                if (options == null) options = new SkillsToolsOptions();

                var dir = skill.SkillDirectory?.Trim();
                if (dir.IsNullOrEmpty())
                {
                    resp.Logs.Add("SkillDirectory 为空");
                    resp.Output = "未配置 SkillDirectory";
                    return resp;
                }
                string fullDir;
                try { 
                    fullDir = Path.GetFullPath(dir); 
                }
                catch { resp.Logs.Add($"SkillDirectory 非法: {dir}"); resp.Output = "SkillDirectory 非法"; return resp; }
                if (!Directory.Exists(fullDir)) { resp.Logs.Add($"目录不存在: {fullDir}"); resp.Output = "Skill 目录不存在"; return resp; }

                progress?.Report("技能: 开始扫描目录…");
                var exts = (options.AllowedScriptExtensions ?? new List<string>()).Select(e => e.ToLowerInvariant()).ToHashSet();
                var files = Directory.EnumerateFiles(fullDir, "*.*", SearchOption.AllDirectories)
                    .Where(p => exts.Contains(Path.GetExtension(p).ToLowerInvariant()))
                    .ToList();
                var tools = new List<object>();
                foreach (var f in files)
                {
                    if (ct.IsCancellationRequested) break;
                    var rel = Path.GetRelativePath(fullDir, f);
                    tools.Add(new { name = Path.GetFileNameWithoutExtension(f), fileName = Path.GetFileName(f), extension = Path.GetExtension(f), relativePath = rel, absolutePath = f });
                }
                var policy = new { options.ScriptTimeoutSeconds, options.MaxOutputSizeBytes, AllowedScriptExtensions = options.AllowedScriptExtensions, AllowedCommands = options.AllowedCommands };
                var resultObj = new { prompt, skill = new { skill.Id, skill.Name, skill.Description, SkillDirectory = fullDir }, tools, policy };
                var toolsJson = JsonConvert.SerializeObject(tools, Formatting.None);
                var outputJson = JsonConvert.SerializeObject(resultObj, Formatting.None);
                resp.Output = outputJson;
                resp.Outputs.Add(new Output { varname = "currentTime", value = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"), type = "string" });
                resp.Outputs.Add(new Output { varname = "skillDirectory", value = fullDir, type = "string" });
                resp.Outputs.Add(new Output { varname = "toolsJson", value = toolsJson, type = "json" });
                resp.Logs.Add($"扫描目录: {fullDir}");
                resp.Logs.Add($"发现工具数: {tools.Count}");
                resp.Logs.Add($"策略: timeout={options.ScriptTimeoutSeconds}s, maxOut={options.MaxOutputSizeBytes} bytes");
                progress?.Report($"技能目录扫描完成，共 {tools.Count} 个工具");

                if (modelConfig != null)
                {
                    resp.Logs.Add($"计划: modelConfig 已提供, tools={tools.Count}, promptLen={(prompt ?? string.Empty).Length}");
                    // 读取 SKILL.md
                    string skillDoc = null;
                    try
                    {
                        var skillDocPath = Path.Combine(fullDir, "SKILL.md");
                        if (File.Exists(skillDocPath))
                        {
                            var raw = File.ReadAllText(skillDocPath, Encoding.UTF8);
                            // 限制最大读取，避免提示超长
                            if (raw != null && raw.Length > 65536) raw = raw.Substring(0, 65536);
                            skillDoc = raw;
                            resp.Logs.Add($"计划: 发现 SKILL.md, size={skillDoc.Length}");
                        }
                        else
                        {
                            resp.Logs.Add("计划: 未发现 SKILL.md");
                        }
                    }
                    catch { resp.Logs.Add("计划: 读取 SKILL.md 失败"); }
                    progress?.Report("技能: 生成计划(JSON)…");
                    string plan = await PlanWithLLMAsync(modelConfig, prompt, toolsJson, policy, skillDoc, ct);
                    var planPreview = plan == null ? "<null>" : (plan.Length > 300 ? plan.Substring(0, 300) + "..." : plan);
                    resp.Logs.Add($"计划: LLM 返回原文: {planPreview}");
                    resp.Outputs.Add(new Output { varname = "results", value = plan ?? string.Empty, type = "json" });
                    if (!plan.IsNullOrEmpty()) resp.Output = plan;
                    resp.Logs.Add("已生成计划(JSON 指令)");
                    progress?.Report("技能: 计划已生成");
                }
                else
                {
                    resp.Logs.Add("计划: 未提供 modelConfig，仅返回 discover 信息");
                }
            }
            catch (Exception ex)
            {
                resp.Logs.Add(ex.Message);
                resp.Output = $"执行出错: {ex.Message}";
            }
            return await Task.FromResult(resp);
        }

        public async Task<AgentSkillResponse> ExecuteAsync(
            Skill skill,
            SkillsToolsOptions options,
            string prompt,
            List<AttachmentItem> attachments = null,
            IProgress<string> progress = null,
            LargeModelConfig modelConfig = null,
            string sessionId = null,
            string processesId = null,
            CancellationToken ct = default)
        {
            var aggregate = new AgentSkillResponse();
            aggregate.Logs.Add("统一执行: 开始 Plan→Execute 流程");
            // 1) 规划
            var planResp = await PlanAsync(skill, options, prompt, attachments, progress, modelConfig, ct);
            if (!string.IsNullOrEmpty(planResp?.Output))
            {
                aggregate.Output = planResp.Output;
            }
            if (planResp?.Outputs != null)
            {
                aggregate.Outputs.AddRange(planResp.Outputs);
            }
            if (planResp?.Logs != null && planResp.Logs.Count > 0)
            {
                aggregate.Logs.AddRange(planResp.Logs);
            }

            // 2) 若是可执行计划则执行
            string planJson = planResp?.Output;
            if (!string.IsNullOrWhiteSpace(planJson))
            {
                if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(planJson, out var __cleaned))
                {
                    planJson = __cleaned;
                }
                try
                {
                    var jo = Newtonsoft.Json.Linq.JObject.Parse(planJson);
                    var mode = jo?["mode"]?.ToString();
                    aggregate.Logs.Add($"统一执行: 计划解析 mode={mode}");
                    if (string.Equals(mode, "execute", StringComparison.OrdinalIgnoreCase))
                    {
                        aggregate.Logs.Add("统一执行: 检测到执行计划，进入 ExecutePlan");
                        var execResp = await ExecutePlanAsync(skill, options, planJson, sessionId, processesId, progress, ct);
                        if (!string.IsNullOrEmpty(execResp?.Output))
                        {
                            aggregate.Outputs.Add(new Output { varname = "execOutput", value = execResp.Output, type = "string" });
                        }
                        if (execResp?.Outputs != null)
                        {
                            aggregate.Outputs.AddRange(execResp.Outputs);
                        }
                        if (execResp?.Logs != null && execResp.Logs.Count > 0)
                        {
                            aggregate.Logs.AddRange(execResp.Logs);
                        }
                    }
                    else
                    {
                        aggregate.Logs.Add("统一执行: 计划非执行模式，已跳过 Execute");
                    }
                }
                catch
                {
                    aggregate.Logs.Add("统一执行: 计划解析失败，跳过 Execute");
                }
            }
            else
            {
                aggregate.Logs.Add("统一执行: 未获得计划内容，跳过 Execute");
            }

            return aggregate;
        }

        public async Task<AgentSkillResponse> ExecuteWithPlanTrackingAsync(
            Skill skill,
            SkillsToolsOptions options,
            string prompt,
            List<AttachmentItem> attachments = null,
            IProgress<string> progress = null,
            LargeModelConfig modelConfig = null,
            string sessionId = null,
            string processesId = null,
            CancellationToken ct = default)
        {
            var resp = new AgentSkillResponse();
            try
            {
                if (skill == null) { resp.Logs.Add("Skill 为空"); resp.Output = "Skill 未提供"; return resp; }
                if (options == null) options = new SkillsToolsOptions();
                var dir = skill.SkillDirectory?.Trim();
                if (dir.IsNullOrEmpty()) { resp.Logs.Add("SkillDirectory 为空"); resp.Output = "未配置 SkillDirectory"; return resp; }
                string fullDir; try { fullDir = Path.GetFullPath(dir); } catch { resp.Logs.Add($"SkillDirectory 非法: {dir}"); resp.Output = "SkillDirectory 非法"; return resp; }
                if (!Directory.Exists(fullDir)) { resp.Logs.Add($"目录不存在: {fullDir}"); resp.Output = "Skill 目录不存在"; return resp; }

                var taskRoot = BuildTaskRoot(fullDir, sessionId, processesId);
                try { if (!Directory.Exists(taskRoot)) Directory.CreateDirectory(taskRoot); } catch { }
                var planPath = Path.Combine(taskRoot, "plan.md");

                PlanModel planModel = null;
                if (!File.Exists(planPath))
                {
                    var planResp = await PlanAsync(skill, options, prompt, attachments, progress, modelConfig, ct);
                    var planJson = planResp?.Output;
                    if (string.IsNullOrWhiteSpace(planJson)) { resp.Logs.Add("未获取到计划"); return resp; }
                    if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(planJson, out var cleaned)) planJson = cleaned;
                    JObject jo = null; 
                    try { jo = JObject.Parse(planJson); } catch { resp.Logs.Add("计划 JSON 非法"); return resp; }
                    planModel = BuildPlanFromJson(jo, fullDir, sessionId, processesId);
                    SavePlanMd(planPath, planModel);
                    resp.Logs.Add($"已创建计划: {planPath}");
                }
                else
                {
                    planModel = LoadPlanMd(planPath);
                    if (planModel == null) { resp.Logs.Add("plan.md 解析失败"); return resp; }
                }

                foreach (var step in planModel.Steps.Where(s => !string.Equals(s.status, "success", StringComparison.OrdinalIgnoreCase)))
                {
                    if (ct.IsCancellationRequested) break;
                    step.status = "running";
                    step.startedAt = DateTime.Now.ToString("s");
                    SavePlanMd(planPath, planModel);

                    var run = new RunCommand
                    {
                        relativePath = step.run.relativePath,
                        args = step.run.args ?? new List<string>(),
                        env = step.run.env ?? new Dictionary<string, string>(),
                        workingDir = step.run.workingDir
                    };

                    var exec = await ExecuteToolAsync(fullDir, taskRoot, run, options, ct);
                    step.exitCode = exec.exitCode;
                    step.stdout = Truncate(exec.stdout, options.MaxOutputSizeBytes);
                    step.stderr = Truncate(exec.stderr, options.MaxOutputSizeBytes);
                    step.tryCount = step.tryCount + 1;
                    step.finishedAt = DateTime.Now.ToString("s");

                    var ok = step.validate != null && string.Equals(step.validate.type, "basic", StringComparison.OrdinalIgnoreCase)
                        ? exec.exitCode == 0
                        : exec.exitCode == 0;

                    step.status = ok ? "success" : "failed";
                    SavePlanMd(planPath, planModel);

                    resp.Logs.Add($"步骤: {step.id} 状态={step.status} 退出码={exec.exitCode}");
                    if (!ok) break;
                }

                planModel.status = planModel.Steps.All(s => string.Equals(s.status, "success", StringComparison.OrdinalIgnoreCase)) ? "completed" :
                                   planModel.Steps.Any(s => string.Equals(s.status, "failed", StringComparison.OrdinalIgnoreCase)) ? "failed" : "in_progress";
                SavePlanMd(planPath, planModel);

                resp.Output = planModel.status;
                resp.Outputs.Add(new Output { varname = "planPath", value = planPath, type = "string" });
                resp.Outputs.Add(new Output { varname = "status", value = planModel.status, type = "string" });
            }
            catch (Exception ex)
            {
                resp.Logs.Add(ex.Message);
                resp.Output = $"执行出错: {ex.Message}";
            }
            return await Task.FromResult(resp);
        }

        public async Task<AgentSkillResponse> ExecutePlanAsync(
            Skill skill,
            SkillsToolsOptions options,
            string planJson,
            string sessionId = null,
            string processesId = null,
            IProgress<string> progress = null,
            CancellationToken ct = default)
        {
            var resp = new AgentSkillResponse();
            try
            {
                if (skill == null) { resp.Logs.Add("Skill 为空"); resp.Output = "Skill 未提供"; return resp; }
                if (options == null) options = new SkillsToolsOptions();
                var dir = skill.SkillDirectory?.Trim();
                if (dir.IsNullOrEmpty()) { resp.Logs.Add("SkillDirectory 为空"); resp.Output = "未配置 SkillDirectory"; return resp; }
                string fullDir; try { fullDir = Path.GetFullPath(dir); } catch { resp.Logs.Add($"SkillDirectory 非法: {dir}"); resp.Output = "SkillDirectory 非法"; return resp; }
                if (!Directory.Exists(fullDir)) { resp.Logs.Add($"目录不存在: {fullDir}"); resp.Output = "Skill 目录不存在"; return resp; }

                if (string.IsNullOrWhiteSpace(planJson)) { resp.Logs.Add("执行: planJson 为空"); return resp; }
                if (ZSN.Utils.Core.Utils.Utils.TryExtractStrictJson(planJson, out var __cleaned))
                {
                    planJson = __cleaned;
                }
                JObject jo = null; try { jo = JObject.Parse(planJson); } catch { resp.Logs.Add("planJson 非法"); return resp; }
                var mode = jo?["mode"]?.ToString();
                resp.Logs.Add($"执行: 解析计划 mode={mode}");
                if (!string.Equals(mode, "execute", StringComparison.OrdinalIgnoreCase))
                {
                    resp.Logs.Add("执行: 计划非执行模式，已忽略");
                    resp.Output = planJson;
                    return resp;
                }
                var runToken = jo?["run"] as JObject;
                if (runToken == null) { resp.Logs.Add("执行: 缺少 run 指令"); return resp; }
                var run = new RunCommand
                {
                    relativePath = runToken?["relativePath"]?.ToString(),
                    workingDir = runToken?["workingDir"]?.ToString(),
                    args = runToken?["args"]?.ToObject<List<string>>() ?? new List<string>(),
                    env = runToken?["env"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>()
                };

                resp.Logs.Add($"执行: run.relativePath={run.relativePath}, argsCount={run.args?.Count ?? 0}, envCount={run.env?.Count ?? 0}");
                progress?.Report($"技能: 执行 {run.relativePath}…");
                var taskRoot = BuildTaskRoot(fullDir, sessionId, processesId);
                resp.Logs.Add($"执行: 任务工作目录={taskRoot}");
                try { if (!Directory.Exists(taskRoot)) Directory.CreateDirectory(taskRoot); } catch { }
                var execRe = await ExecuteToolAsync(fullDir, taskRoot, run, options, ct);
                resp.Outputs.Add(new Output { varname = "results", value = execRe.stdout, type = "string" });
                resp.Outputs.Add(new Output { varname = "stderr", value = execRe.stderr, type = "string" });
                resp.Outputs.Add(new Output { varname = "exitCode", value = execRe.exitCode.ToString(), type = "string" });
                resp.Logs.Add($"已执行: {run.relativePath}, 退出码: {execRe.exitCode}");
                resp.Logs.Add($"工作目录: {taskRoot}");
                progress?.Report($"技能: 执行完成，退出码={execRe.exitCode}");
            }
            catch (Exception ex)
            {
                resp.Logs.Add(ex.Message);
                resp.Output = $"执行出错: {ex.Message}";
            }
            return await Task.FromResult(resp);
        }

        private (string mode, RunCommand run) ParseAdditionalOptions(object additional)
        {
            if (additional == null) return ("discover", null);
            try
            {
                var jo = additional as JObject ?? (JObject)JsonConvert.DeserializeObject(JsonConvert.SerializeObject(additional));
                string mode = jo?["mode"]?.ToString() ?? "discover";
                var runToken = jo?["run"] as JObject;
                RunCommand run = null;
                if (runToken != null)
                {
                    run = new RunCommand
                    {
                        relativePath = runToken?["relativePath"]?.ToString(),
                        workingDir = runToken?["workingDir"]?.ToString(),
                        args = runToken?["args"]?.ToObject<List<string>>() ?? new List<string>(),
                        env = runToken?["env"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>()
                    };
                }
                return (mode, run);
            }
            catch
            {
                return ("discover", null);
            }
        }

        private async Task<string> PlanWithLLMAsync(LargeModelConfig modelConfig, string prompt, string toolsJson, object policy, string skillDoc, CancellationToken ct)
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            var kernel = _kernelService.GetKernel(modelConfig.Model);
            var chat = kernel.GetRequiredService<IChatCompletionService>();
            var settings = PromptExecutionSettingsFactory.Create(modelConfig);
            var history = new ChatHistory();

            var systemPrompt = new StringBuilder();
            systemPrompt.AppendLine("你是Agent的技能规划助手。给定技能工具清单与安全策略，只输出严格的JSON指令用于客户端执行，不要执行。");
            systemPrompt.AppendLine("必须只输出JSON，不要多余文本。");
            systemPrompt.AppendLine("tools=" + toolsJson);
            systemPrompt.AppendLine("policy=" + JsonConvert.SerializeObject(policy));
            if (!string.IsNullOrEmpty(skillDoc))
            {
                systemPrompt.AppendLine("skill_doc=<<<");
                systemPrompt.AppendLine(skillDoc);
                systemPrompt.AppendLine(">>>");
            }

            history.AddSystemMessage(systemPrompt.ToString());
            history.AddUserMessage(prompt ?? string.Empty);

            string result = null;
            string status = "success";
            string errorMsg = null;
            try
            {
                var chatResult = await chat.GetChatMessageContentAsync(history, settings, kernel, ct);
                result = chatResult?.Content?.ConvertToString();
            }
            catch (Exception ex)
            {
                status = "error";
                errorMsg = ex.Message;
                throw;
            }
            finally
            {
                LogAgentSkillCall(modelConfig, prompt, result, sw.ElapsedMilliseconds, status, errorMsg);
            }
            return result;
        }

        private void LogAgentSkillCall(LargeModelConfig modelConfig, string input, string output, long durationMs, string status, string error = null)
        {
            try
            {
                var model = modelConfig.Model;
                var logDetail = JsonConvert.SerializeObject(new
                {
                    serviceName = "AgentSkillService",
                    methodName = "PlanWithLLMAsync",
                    model = new
                    {
                        modelId = model.LargeModelID,
                        modelName = model.ModelName,
                        typeName = model.TypeName,
                        organization = model.ModelOrganizationName,
                        endPoint = model.EndPoint
                    },
                    parameters = new
                    {
                        temperature = modelConfig.Temperature,
                        topP = modelConfig.TopPCoefficient,
                        thinking = modelConfig.Thinking
                    },
                    input = TruncateLog(input),
                    output = TruncateLog(output),
                    timing = new { durationMs },
                    status,
                    error
                }, Formatting.None);
                _logService.AddOperationLog(LLMLogMarkId, logDetail);
            }
            catch { }
        }

        private static string TruncateLog(string text, int maxLength = 10000)
        {
            if (string.IsNullOrEmpty(text)) return text;
            return text.Length > maxLength ? text.Substring(0, maxLength) + "...[truncated]" : text;
        }

        private async Task<(int exitCode, string stdout, string stderr)> ExecuteToolAsync(string rootDir, string taskRoot, RunCommand run, SkillsToolsOptions options, CancellationToken ct)
        {
            // 校验路径
            if (run.relativePath.IsNullOrEmpty()) return (-1, string.Empty, "缺少 relativePath");
            string abs = Path.GetFullPath(Path.Combine(rootDir, run.relativePath));
            if (!abs.StartsWith(Path.GetFullPath(rootDir), StringComparison.OrdinalIgnoreCase))
            {
                return (-1, string.Empty, "路径不安全");
            }
            if (!File.Exists(abs)) return (-1, string.Empty, "文件不存在");

            var ext = Path.GetExtension(abs).ToLowerInvariant();
            if (!(options.AllowedScriptExtensions ?? new List<string>()).Select(x => x.ToLowerInvariant()).Contains(ext))
            {
                return (-1, string.Empty, $"扩展不允许: {ext}");
            }

            // 将脚本复制到 taskRoot 下执行，确保脚本相对路径输出也落在 taskRoot
            string absForExec = abs;
            try
            {
                if (!string.IsNullOrEmpty(taskRoot))
                {
                    var relFromRoot = Path.GetRelativePath(rootDir, abs).Replace('\\', '/');
                    var runMirror = Path.Combine(taskRoot, "__run");
                    var targetPath = Path.Combine(runMirror, relFromRoot);
                    var targetDir = Path.GetDirectoryName(targetPath);
                    if (!Directory.Exists(targetDir)) Directory.CreateDirectory(targetDir);
                    File.Copy(abs, targetPath, true);
                    absForExec = Path.GetFullPath(targetPath);
                }
            }
            catch
            {
                // 若复制失败则退回用原路径执行（仍受工作目录与参数/环境限制保护）
                absForExec = abs;
            }

            // 选择执行器
            var (cmdRaw, argsPrefix) = GetExecutor(ext);
            if (cmdRaw == null) return (-1, string.Empty, $"未支持的执行器: {ext}");
            var cmd = ResolveExecutorPath(cmdRaw);

            // 命令白名单（按原始名称校验，避免绝对路径导致名单不匹配）
            var allowed = (options.AllowedCommands ?? new List<string>()).Select(x => x.ToLowerInvariant()).ToHashSet();
            if (!allowed.Contains(cmdRaw.ToLowerInvariant()))
            {
                return (-1, string.Empty, $"命令未在白名单: {cmdRaw}");
            }

            // 组装参数
            var psi = new ProcessStartInfo
            {
                FileName = cmd,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                WorkingDirectory = string.IsNullOrEmpty(run.workingDir)
                    ? (string.IsNullOrEmpty(taskRoot) ? rootDir : taskRoot)
                    : Path.GetFullPath(Path.Combine(string.IsNullOrEmpty(taskRoot) ? rootDir : taskRoot, run.workingDir))
            };

            // 校验工作目录未越界
            var baseDir = string.IsNullOrEmpty(taskRoot) ? rootDir : taskRoot;
            if (!IsSubPath(baseDir, psi.WorkingDirectory))
            {
                return (-1, string.Empty, "工作目录不安全");
            }

            var argList = new List<string>();
            if (!string.IsNullOrEmpty(argsPrefix))
            {
                argList.AddRange(SplitArgs(argsPrefix));
            }
            argList.Add(absForExec);
            if (run.args != null && run.args.Count > 0)
            {
                var sanitized = new List<string>();
                foreach (var a in run.args)
                {
                    if (string.IsNullOrWhiteSpace(a)) { sanitized.Add(a); continue; }

                    if (LooksLikePathArgument(a))
                    {
                        // 绝对/UNC 路径：仅当位于 baseDir 内部才允许
                        if (Path.IsPathRooted(a) || a.StartsWith("\\\\"))
                        {
                            var full = Path.GetFullPath(a);
                            if (!IsSubPath(baseDir, full))
                            {
                                return (-1, string.Empty, "参数包含不安全路径");
                            }
                            sanitized.Add(full);
                        }
                        else
                        {
                            // 相对路径：归一化到 baseDir
                            var full = Path.GetFullPath(Path.Combine(baseDir, a));
                            if (!IsSubPath(baseDir, full))
                            {
                                return (-1, string.Empty, "参数包含不安全路径");
                            }
                            sanitized.Add(full);
                        }
                    }
                    else
                    {
                        sanitized.Add(a);
                    }
                }
                argList.AddRange(sanitized);
            }

            // 保护性限制参数长度
            var joined = string.Join(' ', argList);
            if (joined.Length > 8192)
            {
                return (-1, string.Empty, "参数过长");
            }

            psi.ArgumentList.Clear();
            foreach (var a in argList) psi.ArgumentList.Add(a);

            // 环境变量过滤
            bool executorIsAbsolute = Path.IsPathRooted(cmd) && File.Exists(cmd);
            if (executorIsAbsolute)
            {
                var preserve = new[] { "PATH", "SystemRoot", "WINDIR", "COMSPEC", "TEMP", "TMP" };
                var snapshot = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (System.Collections.DictionaryEntry kv in Environment.GetEnvironmentVariables())
                {
                    if (kv.Key is string k && kv.Value is string v && preserve.Contains(k, StringComparer.OrdinalIgnoreCase))
                    {
                        snapshot[k] = v;
                    }
                }
                psi.Environment.Clear();
                foreach (var kv in snapshot) psi.Environment[kv.Key] = kv.Value;
            }

            if (run.env != null)
            {
                var deny = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                {
                    "PYTHONPATH", "PYTHONHOME", "PSModulePath", "NODE_PATH"
                };
                foreach (var kv in run.env)
                {
                    if (deny.Contains(kv.Key)) continue;
                    psi.Environment[kv.Key] = kv.Value;
                }
            }

            using var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            var sbOut = new StringBuilder();
            var sbErr = new StringBuilder();

            proc.OutputDataReceived += (s, e) => { if (e.Data != null) AppendWithLimit(sbOut, e.Data + Environment.NewLine, options.MaxOutputSizeBytes); };
            proc.ErrorDataReceived += (s, e) => { if (e.Data != null) AppendWithLimit(sbErr, e.Data + Environment.NewLine, options.MaxOutputSizeBytes); };

            proc.Start();
            proc.BeginOutputReadLine();
            proc.BeginErrorReadLine();

            var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(TimeSpan.FromSeconds(Math.Max(1, options.ScriptTimeoutSeconds)));
            try
            {
                await proc.WaitForExitAsync(cts.Token);
            }
            catch (OperationCanceledException)
            {
                try { if (!proc.HasExited) proc.Kill(true); } catch { }
                sbErr.AppendLine("执行超时");
            }

            var exitCode = proc.HasExited ? proc.ExitCode : -1;
            return (exitCode, sbOut.ToString(), sbErr.ToString());
        }

        private (string cmd, string argsPrefix) GetExecutor(string ext)
        {
            switch (ext)
            {
                case ".ps1":
                    return ("powershell", "-NoProfile -NonInteractive -NoLogo -ExecutionPolicy Bypass -File");
                case ".py":
                    return ("python", "-I -B -E");
                case ".cs":
                    return ("dotnet", null);
                default:
                    return (null, null);
            }
        }

        private string ResolveExecutorPath(string name)
        {
            if (string.IsNullOrEmpty(name)) return name;
            if (string.Equals(name, "powershell", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var sys = Environment.SystemDirectory; // C:\\Windows\\System32
                    var candidate = Path.Combine(sys, "WindowsPowerShell", "v1.0", "powershell.exe");
                    if (File.Exists(candidate)) return candidate;
                }
                catch { }
            }
            return name;
        }

        private static bool IsSubPath(string root, string path)
        {
            try
            {
                var r = Path.GetFullPath(root);
                var p = Path.GetFullPath(path);
                return p.StartsWith(r, StringComparison.OrdinalIgnoreCase);
            }
            catch { return false; }
        }

        private static IEnumerable<string> SplitArgs(string cmdline)
        {
            if (string.IsNullOrWhiteSpace(cmdline)) yield break;
            foreach (var part in cmdline.Split(' ', StringSplitOptions.RemoveEmptyEntries)) yield return part;
        }

        private static bool LooksLikePathArgument(string arg)
        {
            if (string.IsNullOrWhiteSpace(arg)) return false;
            if (arg.StartsWith("\\\\")) return true;
            if (arg.Contains(":")) return true;
            if (arg.Contains("\\") || arg.Contains("/")) return true;
            if (arg.StartsWith(".") || arg.StartsWith("..")) return true;
            return false;
        }

        private static void AppendWithLimit(StringBuilder sb, string text, int maxBytes)
        {
            if (maxBytes <= 0)
            {
                sb.Append(text);
                return;
            }
            var current = Encoding.UTF8.GetByteCount(sb.ToString());
            var incoming = Encoding.UTF8.GetByteCount(text);
            if (current + incoming <= maxBytes)
            {
                sb.Append(text);
            }
            else
            {
                // 近似截断：若超过限制则忽略追加并标注
                if (!sb.ToString().EndsWith("\n[TRUNCATED]\n")) sb.AppendLine("[TRUNCATED]");
            }
        }

        private string Truncate(string s, int maxBytes)
        {
            if (string.IsNullOrEmpty(s)) return s ?? string.Empty;
            if (maxBytes <= 0) return s;
            var bytes = Encoding.UTF8.GetBytes(s);
            if (bytes.Length <= maxBytes) return s;
            var truncated = new byte[maxBytes];
            Array.Copy(bytes, truncated, maxBytes);
            return Encoding.UTF8.GetString(truncated);
        }

        
        private PlanModel BuildPlanFromJson(JObject jo, string fullDir, string sessionId, string processesId)
        {
            var model = new PlanModel
            {
                sessionId = sessionId,
                processesId = processesId,
                skillDirectory = fullDir,
                originalPlanJson = jo.ToString(Formatting.None)
            };

            var steps = new List<PlanStep>();
            var stepsToken = jo["steps"] as JArray;
            if (stepsToken != null)
            {
                int idx = 1;
                foreach (var st in stepsToken.OfType<JObject>())
                {
                    var runT = st["run"] as JObject ?? st;
                    var step = BuildStepFromRun(runT, idx);
                    steps.Add(step);
                    idx++;
                }
            }
            else
            {
                // 兜底1：支持 LLM 输出为 { "plan": [ { filePath: "..." }, ... ] }
                var planToken = jo["plan"] as JArray;
                if (planToken != null)
                {
                    int idx = 1;
                    foreach (var st in planToken.OfType<JObject>())
                    {
                        var filePath = st["filePath"]?.ToString()?.Trim();
                        string rel = string.Empty;
                        if (!string.IsNullOrEmpty(filePath))
                        {
                            rel = NormalizeRelativePath(fullDir, filePath);
                        }
                        else
                        {
                            var tool = st["tool"]?.ToString()?.Trim();
                            if (!string.IsNullOrEmpty(tool))
                            {
                                rel = $"tools/{tool}.ps1";
                            }
                        }

                        if (string.IsNullOrEmpty(rel))
                        {
                            continue;
                        }

                        var argsArr = st["arguments"] as JArray ?? new JArray();
                        var envObj = st["env"] as JObject ?? new JObject();
                        var runT = new JObject
                        {
                            ["relativePath"] = rel,
                            ["args"] = argsArr,
                            ["env"] = envObj,
                            ["workingDir"] = ""
                        };
                        var step = BuildStepFromRun(runT, idx);
                        steps.Add(step);
                        idx++;
                    }
                }
                else
                {
                    // 兜底2：支持单步 { "run": { ... } }
                    var runT = jo["run"] as JObject;
                    if (runT != null)
                    {
                        steps.Add(BuildStepFromRun(runT, 1));
                    }
                }
            }
            if (steps.Count == 0)
            {
                throw new InvalidOperationException("计划中未发现可执行步骤");
            }
            model.Steps = steps;
            return model;
        }

        private PlanStep BuildStepFromRun(JObject runT, int idx)
        {
            var step = new PlanStep
            {
                id = $"step-{idx}",
                name = runT?["relativePath"]?.ToString() ?? $"step-{idx}",
                run = new RunSpec
                {
                    relativePath = runT?["relativePath"]?.ToString(),
                    args = runT?["args"]?.ToObject<List<string>>() ?? new List<string>(),
                    env = runT?["env"]?.ToObject<Dictionary<string, string>>() ?? new Dictionary<string, string>(),
                    workingDir = runT?["workingDir"]?.ToString()
                },
                validate = new ValidateSpec { type = "basic", rule = "exitCode==0" },
                status = "pending"
            };
            return step;
        }

        private string NormalizeRelativePath(string fullDir, string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return string.Empty;
            try
            {
                // 若是绝对路径且位于技能目录内，转换为相对路径
                if (Path.IsPathRooted(path))
                {
                    var rel = Path.GetRelativePath(fullDir, path);
                    if (!rel.StartsWith("..", StringComparison.Ordinal))
                    {
                        // 统一使用正斜杠格式
                        return rel.Replace('\\', '/');
                    }
                }

                // 非技能目录下的绝对路径或裸文件名：
                // 优先使用 tools/<filename>，若存在则返回该路径；否则返回 filename 本身（由执行时安全校验拦截越界）
                var filename = Path.GetFileName(path);
                if (!string.IsNullOrEmpty(filename))
                {
                    var candidate = Path.Combine("tools", filename);
                    var abs = Path.GetFullPath(Path.Combine(fullDir, candidate));
                    if (File.Exists(abs))
                    {
                        return candidate.Replace('\\', '/');
                    }
                    return filename;
                }
            }
            catch { }
            return path;
        }

        private void SavePlanMd(string planPath, PlanModel model)
        {
            var sb = new StringBuilder();
            sb.AppendLine("---");
            sb.AppendLine($"title: {model.title}");
            sb.AppendLine($"createdAt: {model.createdAt}");
            sb.AppendLine($"sessionId: {model.sessionId}");
            sb.AppendLine($"processesId: {model.processesId}");
            sb.AppendLine($"skillDirectory: {model.skillDirectory}");
            sb.AppendLine($"status: {model.status}");
            sb.AppendLine("originalPlanJson: |");
            if (!string.IsNullOrEmpty(model.originalPlanJson))
            {
                foreach (var line in model.originalPlanJson.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                {
                    sb.AppendLine("  " + line);
                }
            }
            sb.AppendLine("---");
            sb.AppendLine();
            sb.AppendLine("<!-- PLAN:JSON -->");
            sb.AppendLine(JsonConvert.SerializeObject(model, Formatting.None));
            sb.AppendLine("<!-- /PLAN:JSON -->");
            sb.AppendLine();
            sb.AppendLine("# Steps");
            foreach (var step in model.Steps)
            {
                var checkbox = string.Equals(step.status, "success", StringComparison.OrdinalIgnoreCase) ? "[x]" : "[ ]";
                sb.AppendLine($"- {checkbox} {step.id}: {step.name}");
                sb.AppendLine($"  - status: {step.status}");
                sb.AppendLine($"  - try: {step.tryCount}/{step.maxTry}");
                sb.AppendLine($"  - exitCode: {step.exitCode}");
                sb.AppendLine($"  - startedAt: {step.startedAt}");
                sb.AppendLine($"  - finishedAt: {step.finishedAt}");
                sb.AppendLine($"  - command: {step.run?.relativePath}");
                sb.AppendLine($"  - args: {JsonConvert.SerializeObject(step.run?.args ?? new List<string>())}");
                sb.AppendLine($"  - env: {JsonConvert.SerializeObject(step.run?.env ?? new Dictionary<string, string>())}");
                sb.AppendLine($"  - workingDir: {step.run?.workingDir}");
                sb.AppendLine("  - stdout: |");
                if (!string.IsNullOrEmpty(step.stdout))
                {
                    foreach (var line in step.stdout.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                    {
                        sb.AppendLine("      " + line);
                    }
                }
                sb.AppendLine("  - stderr: |");
                if (!string.IsNullOrEmpty(step.stderr))
                {
                    foreach (var line in step.stderr.Split(new[] { "\r\n", "\n" }, StringSplitOptions.None))
                    {
                        sb.AppendLine("      " + line);
                    }
                }
                sb.AppendLine($"  - validate: {{\"type\":\"{step.validate?.type}\",\"rule\":\"{step.validate?.rule}\"}}");
                sb.AppendLine($"  - notes: {step.notes}");
                sb.AppendLine();
            }

            var dir = Path.GetDirectoryName(planPath);
            try { if (!Directory.Exists(dir)) Directory.CreateDirectory(dir); } catch { }
            File.WriteAllText(planPath, sb.ToString(), Encoding.UTF8);
        }

        private PlanModel LoadPlanMd(string planPath)
        {
            try
            {
                var text = File.ReadAllText(planPath, Encoding.UTF8);
                var start = text.IndexOf("<!-- PLAN:JSON -->", StringComparison.OrdinalIgnoreCase);
                var end = text.IndexOf("<!-- /PLAN:JSON -->", StringComparison.OrdinalIgnoreCase);
                if (start >= 0 && end > start)
                {
                    var json = text.Substring(start + "<!-- PLAN:JSON -->".Length, end - (start + "<!-- PLAN:JSON -->".Length)).Trim();
                    var model = JsonConvert.DeserializeObject<PlanModel>(json);
                    return model;
                }
            }
            catch { }
            return null;
        }

        private class RunCommand
        {
            public string relativePath { get; set; }
            public List<string> args { get; set; }
            public Dictionary<string, string> env { get; set; }
            public string workingDir { get; set; }
        }
    }
}
