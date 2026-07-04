using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace ZSN.AgentBrook.AutoPublishJob.Pipeline
{
    /// <summary>
    /// 构建产物校验器：检查 setup.exe/dmg/web 产物是否存在、体积是否合理。
    /// 不做内容级深度校验(签名校验等留待二期)。
    /// </summary>
    public class BuildVerifier
    {
        private readonly ILogger<BuildVerifier> _logger;

        public BuildVerifier(ILogger<BuildVerifier> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 校验构建产物。
        /// </summary>
        /// <param name="bundleDir">tauri build 产物目录(src-tauri/target/release/bundle)</param>
        /// <param name="targets">构建目标(nsis/dmg/web)</param>
        /// <returns>找到的产物文件绝对路径列表(供 ArtifactPublisher 归档)</returns>
        public Task<List<string>> VerifyAsync(string bundleDir, IEnumerable<string> targets, Action<LogEntry>? onLog, CancellationToken ct)
        {
            var result = new List<string>();
            var targetList = (targets ?? Enumerable.Empty<string>()).Select(t => t.ToLowerInvariant()).ToList();

            if (targetList.Count == 0)
            {
                onLog?.Invoke(new LogEntry { Line = "[Verify] 未指定构建目标，跳过产物校验", IsError = true });
                return Task.FromResult(result);
            }

            // NSIS → *.exe (setup)
            if (targetList.Contains("nsis"))
            {
                string nsisDir = Path.Combine(bundleDir, "nsis");
                var exes = SafeGetFiles(nsisDir, "*-setup.exe").ToList();
                if (exes.Count == 0) throw new FileNotFoundException($"[Verify] 未找到 NSIS 产物(*-setup.exe): {nsisDir}");
                result.AddRange(exes);
                onLog?.Invoke(new LogEntry { Line = $"[Verify] NSIS 产物: {string.Join(", ", exes.Select(Path.GetFileName))}" });
            }

            // DMG → *.dmg
            if (targetList.Contains("dmg"))
            {
                string dmgDir = Path.Combine(bundleDir, "dmg");
                var dmgs = SafeGetFiles(dmgDir, "*.dmg").ToList();
                if (dmgs.Count == 0) throw new FileNotFoundException($"[Verify] 未找到 DMG 产物(*.dmg): {dmgDir}");
                result.AddRange(dmgs);
                onLog?.Invoke(new LogEntry { Line = $"[Verify] DMG 产物: {string.Join(", ", dmgs.Select(Path.GetFileName))}" });
            }

            // Web → 由 web 构建把 wwwroot 目录作为产物(由 PublishJob 传入具体路径)
            // Web 产物的校验在 PublishJob 内单独处理(此处 targets 含 "web" 时不在此扫描 bundle)

            // 体积合理性(单个安装包至少 1MB，否则很可能构建失败却产生了空壳)
            foreach (var f in result)
            {
                long size = new FileInfo(f).Length;
                if (size < 1L * 1024 * 1024)
                {
                    throw new InvalidDataException($"[Verify] 产物体积异常过小({size} 字节)，疑似构建失败: {f}");
                }
                onLog?.Invoke(new LogEntry { Line = $"[Verify]   {Path.GetFileName(f)} = {size / 1024.0 / 1024.0:F2} MB" });
            }

            if (result.Count == 0 && !targetList.Contains("web"))
            {
                throw new FileNotFoundException($"[Verify] 未找到任何构建产物于 {bundleDir}");
            }

            return Task.FromResult(result);
        }

        private static IEnumerable<string> SafeGetFiles(string dir, string pattern)
        {
            if (!Directory.Exists(dir)) return Enumerable.Empty<string>();
            try { return Directory.EnumerateFiles(dir, pattern, SearchOption.TopDirectoryOnly); }
            catch { return Enumerable.Empty<string>(); }
        }
    }
}
