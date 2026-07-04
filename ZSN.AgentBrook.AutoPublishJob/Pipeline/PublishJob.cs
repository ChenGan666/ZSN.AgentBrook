using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.AutoPublishJob.Pipeline
{
    /// <summary>
    /// 单个发布任务的编排器：fetch → customize → build → verify → publish，
    /// 每个阶段都回写 PublishTaskInfo 的 State/Progress/Logs。
    /// 失败统一捕获，标记 Failed 并(可选)触发 ReCall 回调。
    /// </summary>
    public class PublishJob
    {
        private readonly PublishJobOptions _options;
        private readonly BuildToolsOptions _tools;
        private readonly GitTemplateFetcher _fetcher;
        private readonly AppCustomizer _customizer;
        private readonly ProcessRunner _runner;
        private readonly BuildVerifier _verifier;
        private readonly ArtifactPublisher _publisher;
        private readonly ILogger<PublishJob> _logger;

        public PublishJob(
            IOptions<PublishJobOptions> options,
            IOptions<BuildToolsOptions> tools,
            GitTemplateFetcher fetcher,
            AppCustomizer customizer,
            ProcessRunner runner,
            BuildVerifier verifier,
            ArtifactPublisher publisher,
            ILogger<PublishJob> logger)
        {
            _options = options.Value;
            _tools = tools.Value;
            _fetcher = fetcher;
            _customizer = customizer;
            _runner = runner;
            _verifier = verifier;
            _publisher = publisher;
            _logger = logger;
        }

        public async Task RunAsync(string taskID, CancellationToken ct)
        {
            PublishTaskInfo task = PublishTaskInfoBusiness.GetModel(taskID)
                ?? throw new InvalidOperationException("任务不存在: " + taskID);

            // 记录开始时间
            task.StartTime = DateTime.Now;
            task.UpdateTime = DateTime.Now;
            PublishTaskInfoBusiness.Update(task);

            Action<LogEntry> onLog = entry =>
            {
                try { PublishTaskInfoBusiness.AppendLog(taskID, task.State, task.Progress, task.Stage, entry.Line); } catch { }
            };

            try
            {
                // ---- 阶段 1: 克隆模板 ----
                SetStage(task, PublishTaskState.Cloning, 5, "克隆模板");
                // 用短哈希(任务ID前12位)做子目录名，避免完整 GUID 导致 Rust 编译路径超 Windows MAX_PATH(260)
                string shortId = taskID.Replace("-", "").Substring(0, Math.Min(12, taskID.Replace("-", "").Length));
                // 关键：Path.GetFullPath 规范化路径分隔符。
                // appsettings 里路径可能用正斜杠(W:/AP/WS)，Path.Combine 后会变成正反斜杠混用
                // (W:/AP/WS\tg_xxx)。Rust/cargo 在 Windows 上对混合分隔符路径调用 RemoveDirectory
                // 会报 "参数错误 (os error 87)"，导致构建脚本清理 out/probe 等目录失败。
                // GetFullPath 统一为反斜杠，从根源消除该问题。
                string taskWorkspace = Path.GetFullPath(Path.Combine(_options.WorkspaceRoot, shortId));
                CleanDir(taskWorkspace);
                string subPath = task.PublishConfig?.templateSubPath ?? "";
                await _fetcher.FetchAsync(task.TemplateGitUrl, task.TemplateRef, subPath, taskWorkspace, onLog, ct);

                // ---- 阶段 2: 定制品牌 ----
                SetStage(task, PublishTaskState.Customizing, 20, "定制品牌(换皮)");
                await _customizer.CustomizeAsync(taskWorkspace, task.PublishConfig ?? new PublishConfig(), onLog, ct);

                // ---- 阶段 3: 构建 ----
                SetStage(task, PublishTaskState.Building, 30, "构建打包");
                var targets = ParseTargets(task.PublishConfig?.build?.targets, task.TargetPlatforms);
                List<string> artifactFiles = new List<string>();
                string? webDir = null;
                // 前端根自适应：client-app/ 子目录 或 工作区根本身
                string clientAppDir = ResolveClientRoot(taskWorkspace);
                string srcTauriDir = Path.Combine(clientAppDir, "src-tauri");
                // Rust 编译产物(target/)指向一个独立的短路径，避免在工作区深路径下触发
                // Windows MAX_PATH 限制(os error 87 failed to remove temp dir)。
                // CARGO_TARGET_DIR 会被 cargo/tauri 自动识别。
                string cargoTargetDir = Path.GetFullPath(Path.Combine(_options.WorkspaceRoot, "tg_" + shortId));
                Environment.SetEnvironmentVariable("CARGO_TARGET_DIR", cargoTargetDir);
                // 桌面构建带 --target x86_64-pc-windows-msvc，cargo 产物路径会多一层 target：
                //   <CARGO_TARGET_DIR>/<target>/release/bundle  (而非 <CARGO_TARGET_DIR>/release/bundle)
                const string desktopTarget = "x86_64-pc-windows-msvc";
                string bundleDir = Path.Combine(cargoTargetDir, desktopTarget, "release", "bundle");
                TimeSpan buildTimeout = TimeSpan.FromSeconds(Math.Max(60, _options.BuildTimeoutSeconds));

                // 构建前清理：删除前端根下的 node_modules(若存在)。
                // 模板仓库可能误提交了 node_modules，或前次失败留下半成品；
                // 残留的 node_modules\npm 会让 npm.cmd 误解析到本地损坏的 npm，
                // 报 "Cannot find module ...\node_modules\npm\bin\npm-cli.js"。
                string nodeModulesDir = Path.Combine(clientAppDir, "node_modules");
                if (Directory.Exists(nodeModulesDir))
                {
                    onLog?.Invoke(new LogEntry { Line = $"[Build] 清理残留 node_modules: {nodeModulesDir}" });
                    TryDeleteDirectory(nodeModulesDir);
                }

                // 3a. 依赖安装(npm install)
                // 用 install 而非 ci：ci 要求 package-lock.json 严格存在，而模板仓库通常不带
                // lockfile(由构建时按 package.json 解析)。install 对此更宽容。
                await RunNpm("install --no-audit --no-fund", clientAppDir, buildTimeout, onLog, ct);

                // 3b. 按目标构建
                bool wantWeb = targets.Contains("web");
                bool wantDesktop = targets.Contains("nsis") || targets.Contains("dmg");

                if (wantWeb)
                {
                    await RunNpm("run build:web", clientAppDir, buildTimeout, onLog, ct);
                    // build:web 产物输出位置(vite.config outDir: '../wwwroot')是相对【前端根】的，
                    // 所以实际在 clientAppDir 的父目录下。探测多个候选位置：
                    //   1) clientAppDir/../wwwroot  ← vite outDir='../wwwroot' 的实际落点
                    //   2) clientAppDir/dist        ← outDir='dist' 兜底
                    //   3) taskWorkspace/wwwroot    ← 旧 client-app 结构下 taskWorkspace=前端根父目录
                    string[] candidates = {
                        Path.GetFullPath(Path.Combine(clientAppDir, "..", "wwwroot")),
                        Path.Combine(clientAppDir, "dist"),
                        Path.Combine(taskWorkspace, "wwwroot"),
                    };
                    webDir = candidates.FirstOrDefault(Directory.Exists);
                    if (webDir == null)
                    {
                        throw new Exception(
                            $"Web 构建后未找到产物目录(已尝试:\n  {string.Join("\n  ", candidates)}\n)。请检查 vite.config 的 outDir 配置。");
                    }
                    onLog?.Invoke(new LogEntry { Line = $"[Build] Web 产物目录: {webDir}" });
                }
                if (wantDesktop)
                {
                    // Tauri 桌面构建需要 Rust + MSVC C++ 工具链。
                    // 关键点:
                    //   1) ARM64 Windows(rustc host=aarch64)上默认 target 是 ARM64,需强制 x86_64-pc-windows-msvc
                    //   2) cc-rs 编译 native C 代码需要 MSVC 的 cl.exe,它不在 PATH,必须先加载 vcvars64.bat 环境
                    await RunDesktopBuild(clientAppDir, buildTimeout, onLog, ct);
                }

                // ---- 阶段 4: 校验 ----
                SetStage(task, PublishTaskState.Verifying, 80, "校验产物");
                var desktopTargets = targets.Where(t => t == "nsis" || t == "dmg").ToList();
                if (desktopTargets.Count > 0 && Directory.Exists(bundleDir))
                {
                    var verified = await _verifier.VerifyAsync(bundleDir, desktopTargets, onLog, ct);
                    artifactFiles.AddRange(verified);
                }
                else if (desktopTargets.Count > 0)
                {
                    throw new DirectoryNotFoundException($"[Verify] 构建产物目录不存在: {bundleDir}，桌面构建可能失败");
                }

                // ---- 阶段 5: 归档 ----
                SetStage(task, PublishTaskState.Verifying, 90, "归档产物");
                string productName = task.PublishConfig?.brand?.productName ?? task.TemplateName;
                string archiveDir = await _publisher.PublishAsync(taskID, productName, artifactFiles, webDir, onLog, ct);

                // ---- 完成 ----
                PublishTaskInfoBusiness.MarkDone(taskID, archiveDir, "");
                AppendFinalLog(taskID, $"✅ 发布完成，产物目录: {archiveDir}");
                TryReCall(task, success: true);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "[PublishJob] 任务失败 TaskID={TaskID}", taskID);
                AppendFinalLog(taskID, $"❌ 失败: {ex.Message}");
                PublishTaskInfoBusiness.MarkFailed(taskID, ex.Message);
                TryReCall(task, success: false);
            }
        }

        private void SetStage(PublishTaskInfo task, PublishTaskState state, int progress, string stage)
        {
            task.State = state;
            task.Progress = progress;
            task.Stage = stage;
            task.UpdateTime = DateTime.Now;
            PublishTaskInfoBusiness.Update(task);
            _logger.LogInformation("[PublishJob] 阶段切换: {Stage}", stage);
        }

        private void AppendFinalLog(string taskID, string line)
        {
            try { PublishTaskInfoBusiness.AppendLog(taskID, PublishTaskState.Done, 100, "完成", line); } catch { }
        }

        /// <summary>
        /// 解析构建目标：优先取 PublishConfig.build.targets，否则按 TargetPlatforms 字符串推断。
        /// </summary>
        private List<string> ParseTargets(List<string>? configTargets, string targetPlatformsStr)
        {
            if (configTargets != null && configTargets.Count > 0)
            {
                return configTargets.Select(t => t.ToLowerInvariant()).Distinct().ToList();
            }
            var result = new List<string>();
            string tp = (targetPlatformsStr ?? "").ToLowerInvariant();
            if (tp.Contains("win")) result.Add("nsis");
            if (tp.Contains("mac")) result.Add("dmg");
            if (tp.Contains("web")) result.Add("web");
            if (result.Count == 0) result.Add("nsis"); // 兜底
            return result;
        }

        private static void CleanDir(string dir)
        {
            if (Directory.Exists(dir)) Directory.Delete(dir, recursive: true);
            Directory.CreateDirectory(dir);
        }

        /// <summary>
        /// 健壮删除目录(node_modules 常因长路径/文件锁导致普通 Delete 失败)：
        /// 先尝试普通删除，失败则按只读属性复位后重试，仍失败则跳过(不致命)。
        /// </summary>
        private static void TryDeleteDirectory(string dir)
        {
            if (!Directory.Exists(dir)) return;
            try
            {
                Directory.Delete(dir, recursive: true);
                return;
            }
            catch { /* 长路径或占用，降级处理 */ }
            try
            {
                // 清除只读属性后重试
                foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories))
                {
                    try { File.SetAttributes(f, FileAttributes.Normal); } catch { }
                }
                Directory.Delete(dir, recursive: true);
            }
            catch (Exception ex)
            {
                // node_modules 删不掉不致命(npm ci 会重建)；记录但不抛
                Console.WriteLine($"[TryDeleteDirectory] 删除 {dir} 失败(忽略): {ex.Message}");
            }
        }

        /// <summary>
        /// 执行一个构建步骤，失败(ExitCode!=0)即抛异常中止整个任务。
        /// 之前的 bug：构建命令失败不检查 ExitCode，继续往下跑到校验阶段才报"产物不存在"，
        /// 掩盖了真正的构建错误。这里统一在失败时抛出含 stderr 尾部的清晰异常。
        /// </summary>
        private async Task RunBuildStep(string file, string args, string workDir, TimeSpan timeout, Action<LogEntry>? onLog, CancellationToken ct)
        {
            var result = await _runner.RunAsync(file, args, workDir, timeout, onLog, ct);
            if (!result.Succeeded)
            {
                // 取 stderr 尾部帮助定位(很多工具把真实错误打到 stderr)
                string errTail = result.Error;
                if (!string.IsNullOrEmpty(errTail) && errTail.Length > 1500)
                    errTail = "..." + errTail.Substring(errTail.Length - 1500);
                throw new Exception(
                    $"构建步骤失败: {file} {args} (ExitCode={result.ExitCode})\n" +
                    (string.IsNullOrEmpty(errTail) ? "(无 stderr 输出，请查看上方完整日志)" : $"stderr 尾部:\n{errTail}"));
            }
        }

        /// <summary>
        /// 执行 npm 命令：用 node 直接跑全局 npm-cli.js，绕过 npm.cmd 的 cwd 解析陷阱。
        /// 调用形如：node "C:\...\npm-cli.js" ci --no-audit
        /// 这样无论工作目录有什么半成品 node_modules，都不会干扰 npm 的加载。
        /// </summary>
        private Task RunNpm(string npmArgs, string workDir, TimeSpan timeout, Action<LogEntry>? onLog, CancellationToken ct)
        {
            // _tools.Npm = "node"(或配置的 node 路径)；_tools.NpmArgsPrefix = "\"<cli.js绝对路径>\""
            string fullArgs = $"{_tools.NpmArgsPrefix} {npmArgs}";
            return RunBuildStep(_tools.Npm, fullArgs, workDir, timeout, onLog, ct);
        }

        /// <summary>
        /// 桌面构建(tauri build)：强制 x86_64 target + 在 cmd 里加载 MSVC 环境 + 注入 Clang PATH 后执行。
        ///   1) rustup target add x86_64-pc-windows-msvc (ARM64 Windows 上必须显式 x64)
        ///   2) call vcvarsall.bat x64 (加载 MSVC: cl.exe/link.exe)
        ///   3) 把 VS 自带的 Llvm\bin 加入 PATH (cc-rs 需要 clang，否则报 "failed to find tool clang")
        ///   4) npm run tauri:build -- --target x86_64-pc-windows-msvc
        /// ProcessRunner 用 UseShellExecute=false 无法 source 批处理，故用 cmd.exe /c 串联。
        /// </summary>
        private async Task RunDesktopBuild(string workDir, TimeSpan timeout, Action<LogEntry>? onLog, CancellationToken ct)
        {
            // 1. 确保 x64 target 已安装(ARM64 机器上 host 是 aarch64，需要显式 x64)
            try { await _runner.RunAsync("rustup", "target add x86_64-pc-windows-msvc", workDir, TimeSpan.FromMinutes(3), onLog, ct); }
            catch (Exception ex) { onLog?.Invoke(new LogEntry { Line = $"[Build] rustup target add 跳过(可能已装): {ex.Message}", IsError = true }); }

            // 2. 探测 MSVC 工具链(vcvarsall.bat + clang 目录)
            var msvc = _tools.ResolveMsvc();
            if (string.IsNullOrEmpty(msvc.VcVarsAll))
            {
                throw new Exception(
                    "未找到 MSVC C++ 工具链(vcvarsall.bat)。Tauri 在 Windows 上构建 native 代码需要它。\n" +
                    "请在打包机安装 Visual Studio 2022(Community 即可)并勾选「使用 C++ 的桌面开发」工作负载(含 LLVM/clang),\n" +
                    "或在 appsettings.json 的 BuildTools:VcVarsPath 显式指定 vcvarsall.bat 路径。");
            }
            onLog?.Invoke(new LogEntry { Line = $"[Build] MSVC: {msvc.VcVarsAll}" });
            if (!string.IsNullOrEmpty(msvc.ClangDir))
                onLog?.Invoke(new LogEntry { Line = $"[Build] Clang: {msvc.ClangDir}" });
            else
                onLog?.Invoke(new LogEntry { Line = "[Build] 警告: 未找到 clang，cc-rs 编译 C 代码可能失败(需 VS C++ 工作负载含 LLVM 组件)", IsError = true });

            // 3. 用 cmd.exe 串联：
            //    call vcvarsall.bat x64  &&  set PATH=<clang>;%PATH%  &&  node npm-cli.js run tauri:build -- --target x86_64-pc-windows-msvc
            string cliJs = _tools.NpmArgsPrefix.Trim('"');
            string nodeExe = _tools.Npm.TrimStart('"').TrimEnd('"');
            var parts = new System.Collections.Generic.List<string>
            {
                $"call \"{msvc.VcVarsAll}\" x64"
            };
            if (!string.IsNullOrEmpty(msvc.ClangDir))
            {
                parts.Add($"set \"PATH={msvc.ClangDir};%PATH%\"");
            }
            // 关键：从 PATH 移除 Git\usr\bin(它带 Unix 版 link/sort 等，会劫持 MSVC 的 link.exe，
            // 导致 Rust 链接报 "/usr/bin/link: missing operand")。
            parts.Add(@"set ""PATH=%PATH:C:\Program Files\Git\usr\bin;=%""");
            // 再把 MSVC 的 cl/link 所在 bin 强制 prepend 到最前，确保 link.exe 一定命中 MSVC 版。
            if (!string.IsNullOrEmpty(msvc.ClBinDir))
            {
                parts.Add($"set \"PATH={msvc.ClBinDir};%PATH%\"");
                onLog?.Invoke(new LogEntry { Line = $"[Build] MSVC bin (link.exe): {msvc.ClBinDir}" });
            }
            parts.Add($"\"{nodeExe}\" \"{cliJs}\" run tauri:build -- --target x86_64-pc-windows-msvc");
            string cmd = string.Join(" && ", parts);
            var result = await _runner.RunAsync("cmd.exe", $"/c \"{cmd}\"", workDir, timeout, onLog, ct);
            if (!result.Succeeded)
            {
                string errTail = result.Error;
                if (!string.IsNullOrEmpty(errTail) && errTail.Length > 1500)
                    errTail = "..." + errTail.Substring(errTail.Length - 1500);
                throw new Exception($"桌面构建失败(tauri build): ExitCode={result.ExitCode}\n{(string.IsNullOrEmpty(errTail) ? "(无 stderr)" : $"stderr 尾部:\n{errTail}")}");
            }
        }

        /// <summary>
        /// 探测前端根目录(与 AppCustomizer.ResolveClientRoot 逻辑一致)：
        ///   - &lt;workspace&gt;/client-app 存在且含 package.json → 用它
        ///   - 否则 &lt;workspace&gt; 含 package.json → 用工作区本身
        /// </summary>
        private static string ResolveClientRoot(string taskWorkspace)
        {
            string sub = Path.Combine(taskWorkspace, "client-app");
            if (Directory.Exists(sub) && File.Exists(Path.Combine(sub, "package.json"))) return sub;
            if (File.Exists(Path.Combine(taskWorkspace, "package.json"))) return taskWorkspace;
            throw new DirectoryNotFoundException(
                $"未找到有效前端目录(尝试过 {sub} 与 {taskWorkspace})，请确认模板结构。");
        }

        /// <summary>
        /// 完成回调(仿 MarkdownJob 的 reCallUrl 机制)，通知提交方任务结束。
        /// 失败不致命。
        /// </summary>
        private async void TryReCall(PublishTaskInfo task, bool success)
        {
            if (string.IsNullOrWhiteSpace(task.ReCallUrl)) return;
            try
            {
                using var http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
                var payload = new { taskID = task.TaskID, state = task.State.ToString(), success, artifactPath = task.ArtifactPath };
                var content = new StringContent(System.Text.Json.JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json");
                var resp = await http.PostAsync(task.ReCallUrl, content);
                _logger.LogInformation("[PublishJob] ReCall 已通知");
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "[PublishJob] ReCall 失败(忽略)");
            }
        }
    }
}
