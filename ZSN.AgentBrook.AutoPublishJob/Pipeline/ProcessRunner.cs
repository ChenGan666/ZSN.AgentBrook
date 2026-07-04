using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZSN.AgentBrook.AutoPublishJob.Pipeline
{
    /// <summary>
    /// 单条日志记录(供 AppendLog 回调使用)
    /// </summary>
    public sealed class LogEntry
    {
        public string Line { get; init; } = "";
        public bool IsError { get; init; }
    }

    /// <summary>
    /// 进程执行结果
    /// </summary>
    public sealed class ProcessResult
    {
        public int ExitCode { get; init; }
        public bool Succeeded => ExitCode == 0;
        public string Output { get; init; } = "";
        public string Error { get; init; } = "";
    }

    /// <summary>
    /// 通用外部进程执行器。
    /// 逐行流式读取 stdout/stderr 并通过回调输出，避免长时间构建(几十分钟)只看到最终结果。
    /// 流式输出 + CancellationToken 超时 + process.Kill(true) 整树清理。
    /// </summary>
    public class ProcessRunner
    {
        private readonly ILogger<ProcessRunner> _logger;

        public ProcessRunner(ILogger<ProcessRunner> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 执行一个命令行进程。
        /// </summary>
        /// <param name="fileName">可执行文件(或通过 options 解析后的完整路径)</param>
        /// <param name="arguments">命令行参数</param>
        /// <param name="workingDirectory">工作目录</param>
        /// <param name="timeout">超时(超时则杀死进程并抛 TimeoutException)</param>
        /// <param name="onLog">逐行日志回调(stdout/stderr 都会回调)</param>
        /// <param name="cancellationToken">取消令牌</param>
        public async Task<ProcessResult> RunAsync(
            string fileName,
            string arguments,
            string? workingDirectory = null,
            TimeSpan? timeout = null,
            Action<LogEntry>? onLog = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                throw new ArgumentException("fileName 不能为空", nameof(fileName));

            var psi = new ProcessStartInfo
            {
                FileName = fileName,
                Arguments = arguments,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8,
            };
            if (!string.IsNullOrWhiteSpace(workingDirectory))
                psi.WorkingDirectory = Path.GetFullPath(workingDirectory);

            // 传入 PATH 以便 git/npm/tauri 等能找到各自依赖
            foreach (System.Collections.DictionaryEntry env in Environment.GetEnvironmentVariables())
            {
                if (env.Key is string k && env.Value is string v && !psi.EnvironmentVariables.ContainsKey(k))
                {
                    psi.EnvironmentVariables[k] = v;
                }
            }

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };

            var stdoutBuilder = new StringBuilder();
            var stderrBuilder = new StringBuilder();

            process.OutputDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                stdoutBuilder.AppendLine(e.Data);
                onLog?.Invoke(new LogEntry { Line = e.Data, IsError = false });
            };
            process.ErrorDataReceived += (s, e) =>
            {
                if (e.Data == null) return;
                stderrBuilder.AppendLine(e.Data);
                onLog?.Invoke(new LogEntry { Line = e.Data, IsError = true });
            };

            // 超时/取消 → 杀整棵进程树(npm/tauri 会派生子进程)
            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout.HasValue) linkedCts.CancelAfter(timeout.Value);

            using var reg = linkedCts.Token.Register(() =>
            {
                try { if (!process.HasExited) process.Kill(entireProcessTree: true); } catch { }
            });

            _logger.LogInformation("[ProcessRunner] 启动子进程");

            try
            {
                if (!process.Start())
                    throw new InvalidOperationException($"进程启动返回 false: {fileName}");
            }
            catch (System.ComponentModel.Win32Exception wex) when (wex.NativeErrorCode == 2)
            {
                // 错误码 2 = "系统找不到指定的文件"：通常是命令不在 PATH，或 Windows 上漏了 .cmd/.bat 后缀
                throw new System.ComponentModel.Win32Exception(
                    $"找不到可执行文件 '{fileName}'(工作目录: {workingDirectory ?? "(inherit)"})。\n" +
                    $"可能原因：1) 未安装该工具或不在 PATH；2) Windows 上脚本命令(npm/tauri)需用 .cmd 后缀。\n" +
                    $"可在 appsettings.json 的 BuildTools 节显式指定完整路径。", wex);
            }

            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            string output = stdoutBuilder.ToString();
            string error = stderrBuilder.ToString();

            _logger.LogInformation("[ProcessRunner] 子进程结束");

            if (linkedCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
            {
                // 由超时触发
                throw new TimeoutException($"进程超时被杀死: {fileName} {arguments}");
            }

            return new ProcessResult
            {
                ExitCode = process.ExitCode,
                Output = output,
                Error = error
            };
        }
    }
}
