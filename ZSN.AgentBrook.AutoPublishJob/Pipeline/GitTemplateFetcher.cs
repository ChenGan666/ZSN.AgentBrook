using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ZSN.AgentBrook.AutoPublishJob.Pipeline
{
    /// <summary>
    /// 模板仓库拉取器：git clone 到本地缓存，按 url 复用，无缓存时才联网。
    /// 把模板内容复制到任务专属工作区供后续定制+构建使用(不污染缓存)。
    ///
    /// 支持单仓库多模板：若 <paramref name="subPath"/> 非空，只复制仓库内该子目录
    /// (适用于一个 Git 仓库里放 BaseApp/ MeetingApp/ 多个模板的场景)。
    /// </summary>
    public class GitTemplateFetcher
    {
        private readonly ProcessRunner _runner;
        private readonly IOptions<PublishJobOptions> _options;
        private readonly IOptions<BuildToolsOptions> _tools;
        private readonly ILogger<GitTemplateFetcher> _logger;

        public GitTemplateFetcher(ProcessRunner runner, IOptions<PublishJobOptions> options, IOptions<BuildToolsOptions> tools, ILogger<GitTemplateFetcher> logger)
        {
            _runner = runner;
            _options = options;
            _tools = tools;
            _logger = logger;
        }

        /// <summary>
        /// 取模板到任务工作区。
        /// </summary>
        /// <param name="gitUrl">模板仓库地址(必须是 git 可克隆的地址，非网页 URL)</param>
        /// <param name="ref">分支/tag/commit(空则用默认分支)</param>
        /// <param name="subPath">仓库内子目录(如 "MeetingApp")；空则复制整个仓库</param>
        /// <param name="taskWorkspace">任务工作区目录(模板内容将放入此目录)</param>
        /// <param name="onLog">日志回调</param>
        public async Task FetchAsync(string gitUrl, string? @ref, string? subPath, string taskWorkspace, Action<LogEntry>? onLog, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(gitUrl))
                throw new ArgumentException("模板 Git 地址为空", nameof(gitUrl));

            // 校验 URL 是可克隆地址而非网页 URL
            string normalizedUrl = gitUrl.Trim();
            if (normalizedUrl.Contains("/tree/", StringComparison.Ordinal) || normalizedUrl.Contains("/blob/", StringComparison.Ordinal))
            {
                throw new ArgumentException(
                    $"模板 Git 地址格式错误：'{normalizedUrl}' 看起来是网页 URL(含 /tree/ 或 /blob/)。\n" +
                    "请使用仓库的可克隆地址。GitHub 示例：https://github.com/<user>/<repo>.git\n" +
                    "若仓库内有子目录(如 MeetingApp)，请在模板配置里填写 SubPath，而不是把子路径拼进 URL。");
            }

            string cleanSubPath = (subPath ?? "").Trim().Trim('/', '\\');
            if (!string.IsNullOrEmpty(cleanSubPath))
                onLog?.Invoke(new LogEntry { Line = $"[Fetch] 子目录: {cleanSubPath}" });

            Directory.CreateDirectory(_options.Value.TemplateCacheRoot);
            Directory.CreateDirectory(taskWorkspace);

            string cacheDir = GetCacheDir(gitUrl);
            string gitBin = _tools.Value.Git;
            // safe.directory 信任前缀：缓存目录可能在 UNC/网络挂载/非当前用户创建的文件系统上，
            // git 会以 "dubious ownership" 拒绝操作。对作用在 cacheDir 上的命令注入 -c 内联信任，
            // 避免依赖全局 config(全局 config 会累积且需额外写文件)。
            string safeArgs = $"-c safe.directory=\"{cacheDir}\" -c safe.directory=\"*\"";

            // 1. 缓存不存在 → clone；已存在 → fetch 更新
            bool cacheExists = Directory.Exists(Path.Combine(cacheDir, ".git"));
            if (!cacheExists)
            {
                onLog?.Invoke(new LogEntry { Line = $"[Fetch] 首次克隆模板 → {normalizedUrl}" });
                var cloneResult = await _runner.RunAsync(gitBin, $"clone --progress \"{normalizedUrl}\" \"{cacheDir}\"", null, TimeSpan.FromMinutes(10), onLog, ct);
                if (cloneResult.ExitCode != 0)
                {
                    throw new Exception(
                        $"git clone 失败 (ExitCode={cloneResult.ExitCode})，URL: {normalizedUrl}\n" +
                        $"错误输出: {cloneResult.Error}\n" +
                        "请确认：1) 地址正确且以 .git 结尾；2) 打包机能访问该仓库；3) 私有仓库已配置 git 凭据。");
                }
                if (!Directory.Exists(Path.Combine(cacheDir, ".git")))
                {
                    throw new Exception($"git clone 后未找到 .git 目录，克隆可能未成功: {cacheDir}");
                }
            }
            else
            {
                onLog?.Invoke(new LogEntry { Line = $"[Fetch] 模板缓存命中，执行 fetch 更新" });
                try
                {
                    var fetchResult = await _runner.RunAsync(gitBin, $"{safeArgs} fetch --all --prune --progress", cacheDir, TimeSpan.FromMinutes(5), onLog, ct);
                    if (fetchResult.ExitCode != 0)
                    {
                        onLog?.Invoke(new LogEntry { Line = $"[Fetch] fetch 返回非零(将沿用缓存): ExitCode={fetchResult.ExitCode}", IsError = true });
                    }
                }
                catch (Exception ex)
                {
                    // fetch 失败(离线)不致命，继续用缓存内容
                    onLog?.Invoke(new LogEntry { Line = $"[Fetch] fetch 更新失败(将沿用缓存): {ex.Message}", IsError = true });
                }
            }

            // 2. checkout 到指定 ref + 强制对齐远程(确保缓存 working tree 是最新内容)
            // 关键：fetch 只更新 origin/<ref>(远程跟踪)，本地分支 <ref> 不会自动前进。
            // 若只 checkout <ref>(本地分支)，切到的仍是旧 commit，文件不会更新。
            // 必须 reset --hard origin/<ref> 把本地分支强制快进到远程最新。
            if (!string.IsNullOrWhiteSpace(@ref))
            {
                onLog?.Invoke(new LogEntry { Line = $"[Fetch] checkout → {@ref}" });
                var coResult = await _runner.RunAsync(gitBin, $"{safeArgs} checkout --force {@ref}", cacheDir, TimeSpan.FromMinutes(2), onLog, ct);
                if (coResult.ExitCode != 0)
                {
                    throw new Exception($"git checkout 失败 (ExitCode={coResult.ExitCode})，ref: {@ref}\n错误: {coResult.Error}\n请确认该分支/tag/commit 在仓库中存在。");
                }
                // 对分支执行 reset --hard origin/<ref>，把本地分支强制对齐远程最新(fetch 已更新 origin/<ref>)。
                // 对 tag/commit 此命令会失败(无 origin/<tag>)，忽略即可——tag/commit 内容固定，无需同步。
                var resetResult = await _runner.RunAsync(gitBin, $"{safeArgs} reset --hard origin/{@ref}", cacheDir, TimeSpan.FromMinutes(2), onLog, ct);
                if (resetResult.ExitCode == 0)
                {
                    onLog?.Invoke(new LogEntry { Line = $"[Fetch] 已对齐远程 origin/{@ref} 最新内容" });
                }
                // reset 失败(tag/commit 场景)不报错：checkout 已确保在正确 ref 上
            }

            // 3. 确定源目录(整个仓库 or 子目录)
            string srcDir = cacheDir;
            if (!string.IsNullOrEmpty(cleanSubPath))
            {
                srcDir = Path.Combine(cacheDir, cleanSubPath.Replace('/', Path.DirectorySeparatorChar));
                if (!Directory.Exists(srcDir))
                {
                    throw new Exception($"仓库内未找到子目录: {cleanSubPath} (完整路径: {srcDir})。请检查 SubPath 配置。");
                }
            }

            // 4. 把源目录(剔除 .git)复制到任务工作区
            onLog?.Invoke(new LogEntry { Line = $"[Fetch] 复制模板到工作区: {srcDir} → {taskWorkspace}" });
            CopyDirectoryExcludingGit(srcDir, taskWorkspace);

            onLog?.Invoke(new LogEntry { Line = "[Fetch] 模板就绪" });
        }

        /// <summary>
        /// 缓存目录名 = "tpl_" + url 的短哈希，保证同 url 稳定落到同目录。
        /// </summary>
        private string GetCacheDir(string gitUrl)
        {
            using var sha = SHA256.Create();
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(gitUrl));
            var sb = new StringBuilder();
            for (int i = 0; i < 6; i++) sb.Append(hash[i].ToString("x2"));
            return Path.Combine(_options.Value.TemplateCacheRoot, "tpl_" + sb.ToString());
        }

        /// <summary>
        /// 递归复制目录，跳过 .git 与 node_modules / target / dist / gen。
        /// </summary>
        private static void CopyDirectoryExcludingGit(string sourceDir, string destDir)
        {
            Directory.CreateDirectory(destDir);
            foreach (string file in Directory.GetFiles(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(file);
                File.Copy(file, Path.Combine(destDir, name), overwrite: true);
            }
            foreach (string dir in Directory.GetDirectories(sourceDir, "*", SearchOption.TopDirectoryOnly))
            {
                string name = Path.GetFileName(dir);
                if (name == ".git" || name == "node_modules" || name == "target" || name == "dist" || name == "gen") continue;
                CopyDirectoryExcludingGit(dir, Path.Combine(destDir, name));
            }
        }
    }
}
