using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.Entity;

namespace ZSN.AgentBrook.AutoPublishJob.Pipeline
{
    /// <summary>
    /// 模板定制器：在工作区副本上执行"换皮"。
    /// 1) 占位符替换：扫描模板内的约定占位符文件，按 PublishConfig 替换为真实值；
    /// 2) 资源注入：图标/logo 等二进制资源按约定路径覆盖。
    ///
    /// 设计为"幂等文本替换"，不依赖任何模板专属逻辑，通用基座与会议模板共用。
    ///
    /// 前端根自适应：模板仓库可能把前端放在子目录(如主仓库的 client-app/)，
    /// 也可能仓库根就是前端(如独立模板 MeetingApp/)。本类自动探测：
    ///   - 优先取 &lt;工作区&gt;/client-app (若存在)
    ///   - 否则取 &lt;工作区&gt; 本身 (要求根目录含 package.json)
    /// 所有占位符/资源相对路径都基于探测到的前端根。
    /// </summary>
    public class AppCustomizer
    {
        private readonly ILogger<AppCustomizer> _logger;

        /// <summary>需要进行占位符替换的目标文件(相对【前端根】，不含 client-app/ 前缀)。</summary>
        private static readonly string[] PlaceholderTargets =
        {
            "src-tauri/tauri.conf.json",
            "index.html",
            "src-tauri/src/lib.rs",
            ".env.production",
            ".env.development",
            "src/components/layout/TitleBar.vue"
        };

        public AppCustomizer(ILogger<AppCustomizer> logger)
        {
            _logger = logger;
        }

        public Task CustomizeAsync(string taskWorkspace, PublishConfig config, Action<LogEntry>? onLog, CancellationToken ct)
        {
            if (config == null) throw new ArgumentNullException(nameof(config));

            // ---- 0. 探测前端根 ----
            string clientRoot = ResolveClientRoot(taskWorkspace, onLog);

            // ---- 1. 确定目标 AppID(优先 lockApp.appId，否则回退 connection.appId)----
            string lockedAppId = !string.IsNullOrWhiteSpace(config.lockApp?.appId)
                ? config.lockApp!.appId
                : (config.connection?.appId ?? "");

            // ---- 2. 占位符映射表 ----
            var placeholders = new Dictionary<string, string>(StringComparer.Ordinal)
            {
                ["__APP_PRODUCT_NAME__"] = Safe(config.brand?.productName),
                ["__APP_IDENTIFIER__"] = Safe(config.brand?.identifier),
                ["__APP_VERSION__"] = Safe(config.brand?.version, "1.0.0"),
                ["__APP_TITLE__"] = Safe(config.brand?.appTitle),
                ["__API_BASE_URL__"] = Safe(config.connection?.apiBaseUrl),
                ["__APP_ID__"] = Safe(config.connection?.appId),
                ["__APP_SECRET__"] = Safe(config.connection?.appSecret),
                ["__LOCKED_APP_ID__"] = Safe(lockedAppId),
            };

            // ---- 3. 逐文件替换 ----
            int replacedFiles = 0;
            foreach (string rel in PlaceholderTargets)
            {
                ct.ThrowIfCancellationRequested();
                string abs = Path.Combine(clientRoot, rel.Replace('/', Path.DirectorySeparatorChar));
                if (!File.Exists(abs))
                {
                    onLog?.Invoke(new LogEntry { Line = $"[Customize] 跳过(不存在): {rel}", IsError = false });
                    continue;
                }
                string text = File.ReadAllText(abs);
                string original = text;
                foreach (var kv in placeholders)
                {
                    if (text.Contains(kv.Key))
                    {
                        text = text.Replace(kv.Key, kv.Value);
                    }
                }
                if (text != original)
                {
                    File.WriteAllText(abs, text);
                    replacedFiles++;
                    onLog?.Invoke(new LogEntry { Line = $"[Customize] 占位符替换: {rel}" });
                }
            }
            onLog?.Invoke(new LogEntry { Line = $"[Customize] 共替换 {replacedFiles} 个文件" });

            // ---- 4. tauri.conf.json 结构化字段补强(窗口尺寸等，占位符没覆盖的部分)----
            string tauriConf = Path.Combine(clientRoot, "src-tauri", "tauri.conf.json");
            if (File.Exists(tauriConf) && config.brand != null)
            {
                PatchTauriConf(tauriConf, config.brand, onLog);
            }

            // ---- 5. 二进制资源注入(图标/logo)----
            InjectBinaryResources(clientRoot, config, onLog, ct);

            return Task.CompletedTask;
        }

        /// <summary>
        /// 探测前端根目录：
        ///   - 若 &lt;workspace&gt;/client-app 存在且含 package.json → 用它(主仓库结构)
        ///   - 否则若 &lt;workspace&gt; 含 package.json → 用工作区本身(独立模板结构)
        ///   - 否则抛异常(都不是有效前端项目)
        /// </summary>
        private string ResolveClientRoot(string taskWorkspace, Action<LogEntry>? onLog)
        {
            string sub = Path.Combine(taskWorkspace, "client-app");
            if (Directory.Exists(sub) && File.Exists(Path.Combine(sub, "package.json")))
            {
                onLog?.Invoke(new LogEntry { Line = $"[Customize] 前端根: client-app/ (子目录结构)" });
                return sub;
            }
            if (File.Exists(Path.Combine(taskWorkspace, "package.json")))
            {
                onLog?.Invoke(new LogEntry { Line = $"[Customize] 前端根: 工作区根 (仓库根即前端)" });
                return taskWorkspace;
            }
            throw new DirectoryNotFoundException(
                $"未找到有效的前端目录。已尝试:\n" +
                $"  - {sub} (含 package.json? {File.Exists(Path.Combine(sub, "package.json"))})\n" +
                $"  - {taskWorkspace} (含 package.json? {File.Exists(Path.Combine(taskWorkspace, "package.json"))})\n" +
                $"请确认模板结构：前端项目应在 client-app/ 子目录，或在仓库根。");
        }

        /// <summary>
        /// 用强类型方式修正 tauri.conf.json 中占位符不便表达的窗口尺寸等。
        /// 简单字符串替换 JSON 风险高，这里读取为结构化 JSON 后定点改写。
        /// </summary>
        private void PatchTauriConf(string tauriConfPath, BrandConfig brand, Action<LogEntry>? onLog)
        {
            try
            {
                string json = File.ReadAllText(tauriConfPath);
                var doc = System.Text.Json.JsonDocument.Parse(json);
                using var ms = new MemoryStream();
                using (var writer = new System.Text.Json.Utf8JsonWriter(ms, new System.Text.Json.JsonWriterOptions { Indented = true }))
                {
                    WritePatched(writer, doc.RootElement, brand);
                }
                File.WriteAllText(tauriConfPath, System.Text.Encoding.UTF8.GetString(ms.ToArray()));
                onLog?.Invoke(new LogEntry { Line = $"[Customize] tauri.conf.json 窗口尺寸补强: {brand.windowWidth}x{brand.windowHeight}" });
            }
            catch (Exception ex)
            {
                onLog?.Invoke(new LogEntry { Line = $"[Customize] tauri.conf.json 补强失败(忽略，沿用占位符结果): {ex.Message}", IsError = true });
            }
        }

        /// <summary>递归重写 JSON，命中 windows 数组内 width/height/title 时用品牌值覆盖。</summary>
        private void WritePatched(System.Text.Json.Utf8JsonWriter writer, System.Text.Json.JsonElement el, BrandConfig brand)
        {
            switch (el.ValueKind)
            {
                case System.Text.Json.JsonValueKind.Object:
                    writer.WriteStartObject();
                    foreach (var prop in el.EnumerateObject())
                    {
                        if (prop.Name == "windows" && prop.Value.ValueKind == System.Text.Json.JsonValueKind.Array)
                        {
                            writer.WritePropertyName("windows");
                            writer.WriteStartArray();
                            foreach (var win in prop.Value.EnumerateArray())
                            {
                                writer.WriteStartObject();
                                foreach (var wp in win.EnumerateObject())
                                {
                                    if (wp.Name == "width") { writer.WriteNumber("width", brand.windowWidth <= 0 ? 1200 : brand.windowWidth); }
                                    else if (wp.Name == "height") { writer.WriteNumber("height", brand.windowHeight <= 0 ? 750 : brand.windowHeight); }
                                    else if (wp.Name == "title" && !string.IsNullOrWhiteSpace(brand.windowTitle)) { writer.WriteString("title", brand.windowTitle); }
                                    else { wp.WriteTo(writer); }
                                }
                                writer.WriteEndObject();
                            }
                            writer.WriteEndArray();
                        }
                        else
                        {
                            writer.WritePropertyName(prop.Name);
                            WritePatched(writer, prop.Value, brand);
                        }
                    }
                    writer.WriteEndObject();
                    break;
                case System.Text.Json.JsonValueKind.Array:
                    writer.WriteStartArray();
                    foreach (var item in el.EnumerateArray()) WritePatched(writer, item, brand);
                    writer.WriteEndArray();
                    break;
                default:
                    el.WriteTo(writer);
                    break;
            }
        }

        /// <summary>
        /// 二进制资源注入：若 PublishConfig 提供了图标/logo 的本地路径，覆盖到模板约定位置。
        /// FileCode → 磁盘路径的解析由调用方(管理后台)完成，这里只认路径。
        /// </summary>
        private void InjectBinaryResources(string clientRoot, PublishConfig config, Action<LogEntry>? onLog, CancellationToken ct)
        {
            // 图标
            var iconMap = new (string? source, string relTarget)[]
            {
                (config.icons?.ico, "src-tauri/icons/icon.ico"),
                (config.icons?.icns, "src-tauri/icons/icon.icns"),
                (config.icons?.png128, "src-tauri/icons/128x128.png"),
            };
            foreach (var (source, relTarget) in iconMap)
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(source) && File.Exists(source))
                {
                    string target = Path.Combine(clientRoot, relTarget.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(source, target, overwrite: true);
                    onLog?.Invoke(new LogEntry { Line = $"[Customize] 图标注入: {relTarget}" });
                }
            }

            // Logo
            var logoMap = new (string? source, string relTarget)[]
            {
                (config.branding?.logoUrl, "src/assets/images/logo-b.png"),
                (config.branding?.loginHeroUrl, "src/assets/images/Login_001.png"),
            };
            foreach (var (source, relTarget) in logoMap)
            {
                ct.ThrowIfCancellationRequested();
                if (!string.IsNullOrWhiteSpace(source) && File.Exists(source))
                {
                    string target = Path.Combine(clientRoot, relTarget.Replace('/', Path.DirectorySeparatorChar));
                    Directory.CreateDirectory(Path.GetDirectoryName(target)!);
                    File.Copy(source, target, overwrite: true);
                    onLog?.Invoke(new LogEntry { Line = $"[Customize] 资源注入: {relTarget}" });
                }
            }
        }

        private static string Safe(string? s, string fallback = "") => string.IsNullOrWhiteSpace(s) ? fallback : s!;
    }
}
