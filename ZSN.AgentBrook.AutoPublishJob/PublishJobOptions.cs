using System;
using System.IO;

namespace ZSN.AgentBrook.AutoPublishJob
{
    /// <summary>MSVC 工具链探测结果：vcvarsall.bat + clang 目录 + cl.exe 所在 bin 目录。</summary>
    public sealed record MsvcToolchain(string? VcVarsAll, string? ClangDir, string? ClBinDir);

    /// <summary>
    /// 发布服务运行参数(对应 appsettings.json 的 PublishJob 节)
    /// </summary>
    public class PublishJobOptions
    {
        /// <summary>轮询 tb_publish_task 的间隔(秒)</summary>
        public int PullIntervalSeconds { get; set; } = 3;
        /// <summary>最大并发构建数(构建资源重，建议 1)</summary>
        public int MaxConcurrency { get; set; } = 1;
        /// <summary>工作区根目录(每个任务一个子目录)</summary>
        public string WorkspaceRoot { get; set; } = "./Workspace";
        /// <summary>模板本地缓存目录(git clone 缓存，按 url+ref 复用)</summary>
        public string TemplateCacheRoot { get; set; } = "./Templates";
        /// <summary>产物输出根目录</summary>
        public string ArtifactOutputRoot { get; set; } = "./Artifacts";
        /// <summary>单任务构建超时(秒)</summary>
        public int BuildTimeoutSeconds { get; set; } = 1800;
        /// <summary>日志最大保留行数(防止 Logs 字段无限膨胀)</summary>
        public int MaxLogLines { get; set; } = 5000;
    }

    /// <summary>
    /// 构建工具路径配置(对应 appsettings.json 的 BuildTools 节)。
    /// 各项留空则自动探测。
    ///
    /// 关键：npm 在 Windows 上是 .cmd 批处理，且 npm.cmd 内部用 node 调 cli.js 时，
    /// node 的模块解析会优先在【工作目录】找 node_modules\npm\bin\npm-cli.js。
    /// 若 cwd 有半成品 node_modules(npm install 中途产物)，就报 MODULE_NOT_FOUND。
    /// 根治：用 `node <全局cli.js绝对路径>` 代替 npm.cmd，让 node 直接拿到绝对路径脚本，
    /// 不依赖 cwd 解析。Npm/NpmArgsPrefix 即为此设计。
    /// </summary>
    public class BuildToolsOptions
    {
        public string? GitPath { get; set; }
        public string? NodePath { get; set; }
        /// <summary>npm 的 cli.js 路径(留空则自动探测全局 npm-cli.js)</summary>
        public string? NpmCliPath { get; set; }
        public string? TauriCliPath { get; set; }
        public string? DotnetPath { get; set; }
        /// <summary>MSVC 环境脚本 vcvars64.bat 路径(留空则自动探测 VS 安装目录)</summary>
        public string? VcVarsPath { get; set; }

        // git/node/dotnet 是真正的 .exe，PATH 里能直接找到 → 原样返回裸名。
        public string Git => ResolveExe(GitPath, "git");
        public string Node => ResolveExe(NodePath, "node");
        public string Dotnet => ResolveExe(DotnetPath, "dotnet");

        /// <summary>
        /// npm 的可执行文件：用 node 直接跑全局 cli.js，绕过 npm.cmd 的 cwd 解析陷阱。
        /// 返回 "node"(或配置的 NodePath)。
        /// </summary>
        public string Npm => string.IsNullOrWhiteSpace(NodePath) ? "node" : NodePath;

        /// <summary>
        /// npm 命令的参数前缀：即全局 cli.js 的绝对路径。
        /// 调用形如：node "C:\...\npm-cli.js" ci --no-audit
        /// </summary>
        public string NpmArgsPrefix => "\"" + ResolveNpmCli() + "\"";

        /// <summary>探测全局 npm-cli.js 绝对路径。</summary>
        private string ResolveNpmCli()
        {
            // 1. 显式配置优先
            if (!string.IsNullOrWhiteSpace(NpmCliPath) && File.Exists(NpmCliPath)) return NpmCliPath!;

            // 2. 常见安装位置探测
            string[] candidates;
            if (OperatingSystem.IsWindows())
            {
                var progFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
                candidates = new[]
                {
                    Path.Combine(progFiles, "nodejs", "node_modules", "npm", "bin", "npm-cli.js"),
                    // npm config prefix 下的全局 node_modules
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "npm", "node_modules", "npm", "bin", "npm-cli.js"),
                };
            }
            else
            {
                candidates = new[]
                {
                    "/usr/lib/node_modules/npm/bin/npm-cli.js",
                    "/usr/local/lib/node_modules/npm/bin/npm-cli.js",
                    Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".nvm/versions/node/current/lib/node_modules/npm/bin/npm-cli.js"),
                };
            }
            foreach (var c in candidates)
            {
                if (File.Exists(c)) return c;
            }

            // 3. 兜底：用 where/which 找 npm 再推断(运行时探测)
            try
            {
                var which = OperatingSystem.IsWindows() ? "where" : "which";
                var psi = new System.Diagnostics.ProcessStartInfo(which, "npm")
                {
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                };
                using var p = System.Diagnostics.Process.Start(psi);
                string output = p?.StandardOutput.ReadToEnd() ?? "";
                p?.WaitForExit(3000);
                var firstLine = output.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (firstLine.Length > 0)
                {
                    // npm.cmd 同级或上级的 node_modules/npm/bin/npm-cli.js
                    var dir = Path.GetDirectoryName(firstLine[0].Trim()) ?? "";
                    var guess = Path.Combine(dir, "node_modules", "npm", "bin", "npm-cli.js");
                    if (File.Exists(guess)) return guess;
                }
            }
            catch { }

            // 4. 实在找不到，回退到 npm.cmd(Windows)/npm(其他)，至少能跑且错误可读
            return OperatingSystem.IsWindows() ? "npm.cmd" : "npm";
        }

        // tauri：构建时走 `npm run tauri:build`(npm 内部解析)，不直接调 tauri 命令。
        // 这里保留属性以备将来直接调用，Windows 上补 .cmd。
        public string TauriCli => ResolveScript(TauriCliPath, "tauri");

        private static string ResolveExe(string? configured, string fallback)
            => string.IsNullOrWhiteSpace(configured) ? fallback : configured!;

        private static string ResolveScript(string? configured, string fallback)
        {
            string value = string.IsNullOrWhiteSpace(configured) ? fallback : configured!;
            if (Path.HasExtension(value)) return value;
            if (Path.IsPathRooted(value)) return value;
            if (OperatingSystem.IsWindows()) return value + ".cmd";
            return value;
        }

        /// <summary>
        /// 探测 MSVC 工具链(vcvarsall.bat + clang 目录)。
        /// tauri 编译 native 代码：vcvarsall 提供 cl.exe/link.exe；clang 是 cc-rs 的必需工具
        /// (否则报 "failed to find tool clang")。
        /// </summary>
        public MsvcToolchain ResolveMsvc()
        {
            string? vcvarsAll = null;
            string? clangDir = null;
            string? clBinDir = null;

            // 1. 显式配置优先
            if (!string.IsNullOrWhiteSpace(VcVarsPath) && File.Exists(VcVarsPath)) vcvarsAll = VcVarsPath;
            if (!OperatingSystem.IsWindows()) return new MsvcToolchain(null, null, null);

            // 2. 遍历 VS 安装根
            var vsRoot = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Microsoft Visual Studio");
            if (Directory.Exists(vsRoot))
            {
                string[] editions = { "Community", "Professional", "Enterprise", "BuildTools", "Preview" };
                foreach (var yearDir in Directory.GetDirectories(vsRoot)) // 2022 等
                {
                    foreach (var edition in editions)
                    {
                        string edDir = Path.Combine(yearDir, edition);
                        if (!Directory.Exists(edDir)) continue;
                        // vcvarsall.bat
                        if (vcvarsAll == null)
                        {
                            string p = Path.Combine(edDir, "VC", "Auxiliary", "Build", "vcvarsall.bat");
                            if (File.Exists(p)) vcvarsAll = p;
                        }
                        // VC\Tools\Llvm\<arch>\bin (clang)。优先 x64，其次 ARM64(ARM Win 上 clang 装在此)，
                        // 再兜底任意架构子目录。clang 本身是交叉编译器，ARM64 版 clang 也能编 x64 target。
                        if (clangDir == null)
                        {
                            string llvmRoot = Path.Combine(edDir, "VC", "Tools", "Llvm");
                            if (Directory.Exists(llvmRoot))
                            {
                                // 优先 x64
                                string x64Bin = Path.Combine(llvmRoot, "x64", "bin");
                                if (File.Exists(Path.Combine(x64Bin, "clang.exe"))) clangDir = x64Bin;
                                else
                                {
                                    // 任意架构子目录(ARM64 Windows 上常为 Llvm\ARM64\bin)
                                    foreach (var sub in Directory.GetDirectories(llvmRoot))
                                    {
                                        string bin = Path.Combine(sub, "bin");
                                        if (File.Exists(Path.Combine(bin, "clang.exe"))) { clangDir = bin; break; }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            // 3. 独立 LLVM 兜底
            if (clangDir == null)
            {
                string standalone = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "LLVM", "bin");
                if (File.Exists(Path.Combine(standalone, "clang.exe"))) clangDir = standalone;
            }
            // 4. 探测 cl.exe 所在 bin 目录(MSVC\<ver>\bin\HostX64\x64)，用于强制 prepend PATH，
            //    避免 Git\usr\bin\link.exe 劫持 MSVC 的 link.exe(会导致 Rust 链接报 "missing operand")。
            if (clBinDir == null && !string.IsNullOrEmpty(vcvarsAll))
            {
                // vcvarsAll 在 <edDir>\VC\Auxiliary\Build\，MSVC 工具在 <edDir>\VC\Tools\MSVC\<ver>\bin
                string msvcTools = Path.GetFullPath(Path.Combine(vcvarsAll, "..", "..", "..", "Tools", "MSVC"));
                if (Directory.Exists(msvcTools))
                {
                    foreach (var verDir in Directory.GetDirectories(msvcTools))
                    {
                        string bin = Path.Combine(verDir, "bin", "HostX64", "x64");
                        if (File.Exists(Path.Combine(bin, "link.exe"))) { clBinDir = bin; break; }
                    }
                }
            }
            return new MsvcToolchain(vcvarsAll, clangDir, clBinDir);
        }
    }
}
