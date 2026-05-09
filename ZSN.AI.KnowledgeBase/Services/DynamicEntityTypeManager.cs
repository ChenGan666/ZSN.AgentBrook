using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.KnowledgeBase.Services
{
    /// <summary>
    /// 动态实体类型管理器（支持自动学习和配置持久化）
    /// </summary>
    public class DynamicEntityTypeManager
    {
        private readonly ILogger<DynamicEntityTypeManager> _logger;
        private readonly string _configPath;
        private EntityTypeMappingConfig _config;
        private readonly object _lockObj = new object();
        private DateTime _lastSaveTime = DateTime.MinValue;
        private readonly TimeSpan _saveInterval = TimeSpan.FromMinutes(5);

        public DynamicEntityTypeManager(
            ILogger<DynamicEntityTypeManager> logger,
            string configPath = "Config/entity_type_mapping.json")
        {
            _logger = logger;
            _configPath = configPath;
            _config = LoadConfig();
        }

        /// <summary>
        /// 标准化实体类型（支持动态学习）
        /// </summary>
        public string NormalizeType(string type)
        {
            if (string.IsNullOrWhiteSpace(type))
                return "UNKNOWN";

            var key = type.Trim();
            var keyLower = key.ToLower();

            lock (_lockObj)
            {
                // 1. 尝试静态映射
                if (_config.StaticMappings.TryGetValue(keyLower, out var staticMapping))
                {
                    UpdateStatistics(key, staticMapping);
                    return staticMapping;
                }

                // 2. 尝试学习到的映射
                if (_config.LearnedMappings.TryGetValue(keyLower, out var learnedMapping))
                {
                    UpdateStatistics(key, learnedMapping);
                    return learnedMapping;
                }

                // 3. 未知类型，记录统计
                UpdateStatistics(key, null);

                // 4. 检查是否需要自动学习
                if (_config.AutoLearnEnabled)
                {
                    TryAutoLearn(key);
                }

                // 5. 返回标准化的原始类型
                return key.ToUpper().Replace(" ", "_");
            }
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        private void UpdateStatistics(string type, string? mappedTo)
        {
            var keyLower = type.ToLower();

            if (!_config.Statistics.ContainsKey(keyLower))
            {
                _config.Statistics[keyLower] = new TypeStatistics
                {
                    Frequency = 0,
                    FirstSeen = DateTime.UtcNow,
                    LastSeen = DateTime.UtcNow
                };
            }

            var stats = _config.Statistics[keyLower];
            stats.Frequency++;
            stats.LastSeen = DateTime.UtcNow;

            // 定期保存
            if (DateTime.UtcNow - _lastSaveTime > _saveInterval)
            {
                _ = Task.Run(() => SaveConfigAsync());
            }
        }

        /// <summary>
        /// 尝试自动学习新类型
        /// </summary>
        private void TryAutoLearn(string type)
        {
            var keyLower = type.ToLower();

            if (!_config.Statistics.TryGetValue(keyLower, out var stats))
                return;

            // 达到阈值且未自动添加过
            if (stats.Frequency >= _config.MinFrequencyForAutoAdd &&
                stats.AutoAddedAt == null)
            {
                // 智能推断映射类型
                var suggestedMapping = InferTypeMapping(type);

                if (!string.IsNullOrEmpty(suggestedMapping))
                {
                    _config.LearnedMappings[keyLower] = suggestedMapping;
                    stats.AutoAddedAt = DateTime.UtcNow;
                    stats.SuggestedMapping = suggestedMapping;

                    _logger.LogInformation(
                        "🎓 自动学习新类型映射: {Type} -> {Mapping} (频率: {Frequency})",
                        type, suggestedMapping, stats.Frequency);

                    // 立即保存
                    _ = Task.Run(() => SaveConfigAsync());
                }
            }
        }

        /// <summary>
        /// 智能推断类型映射
        /// </summary>
        private string InferTypeMapping(string type)
        {
            var typeLower = type.ToLower();

            // 规则1: 包含关键词
            var keywordRules = new Dictionary<string, string[]>
            {
                { "PERSON", new[] { "人", "名", "者", "员", "师", "家" } },
                { "ORG", new[] { "公司", "组织", "机构", "团队", "部门", "学校", "大学" } },
                { "PRODUCT", new[] { "产品", "软件", "系统", "平台", "工具", "应用", "框架" } },
                { "TECH", new[] { "技术", "算法", "协议", "方法", "架构" } },
                { "FEATURE", new[] { "功能", "特性", "能力", "模块", "组件" } },
                { "CONCEPT", new[] { "概念", "术语", "定义", "理论" } },
                { "DATABASE", new[] { "数据库", "库" } },
                { "MODEL", new[] { "模型" } },
                { "VERSION", new[] { "版本", "v" } }
            };

            foreach (var (targetType, keywords) in keywordRules)
            {
                if (keywords.Any(kw => typeLower.Contains(kw)))
                {
                    _logger.LogDebug("推断类型: {Type} -> {Target} (匹配关键词)",
                        type, targetType);
                    return targetType;
                }
            }

            // 规则2: 默认归类为CONCEPT
            _logger.LogDebug("推断类型: {Type} -> CONCEPT (默认)", type);
            return "CONCEPT";
        }

        /// <summary>
        /// 手动添加映射
        /// </summary>
        public void AddMapping(string sourceType, string targetType, bool isStatic = false)
        {
            lock (_lockObj)
            {
                var keyLower = sourceType.ToLower();
                var targetUpper = targetType.ToUpper();

                if (isStatic)
                {
                    _config.StaticMappings[keyLower] = targetUpper;
                    _logger.LogInformation("添加静态映射: {Source} -> {Target}",
                        sourceType, targetUpper);
                }
                else
                {
                    _config.LearnedMappings[keyLower] = targetUpper;

                    if (_config.Statistics.TryGetValue(keyLower, out var stats))
                    {
                        stats.AutoAddedAt = DateTime.UtcNow;
                        stats.SuggestedMapping = targetUpper;
                    }

                    _logger.LogInformation("添加学习映射: {Source} -> {Target}",
                        sourceType, targetUpper);
                }

                _ = Task.Run(() => SaveConfigAsync());
            }
        }

        /// <summary>
        /// 获取未映射类型的统计
        /// </summary>
        public List<(string Type, int Frequency, DateTime LastSeen)> GetUnmappedTypeStatistics()
        {
            lock (_lockObj)
            {
                return _config.Statistics
                    .Where(kv => !_config.StaticMappings.ContainsKey(kv.Key) &&
                                 !_config.LearnedMappings.ContainsKey(kv.Key))
                    .OrderByDescending(kv => kv.Value.Frequency)
                    .Select(kv => (kv.Key, kv.Value.Frequency, kv.Value.LastSeen))
                    .ToList();
            }
        }

        /// <summary>
        /// 加载配置
        /// </summary>
        private EntityTypeMappingConfig LoadConfig()
        {
            try
            {
                if (File.Exists(_configPath))
                {
                    //_logger.LogInformation("📂 从文件加载配置: {Path}", _configPath);
                    var json = File.ReadAllText(_configPath);
                    var config = JsonSerializer.Deserialize<EntityTypeMappingConfig>(json,
                        new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

                    if (config != null)
                    {
                        /*
                        _logger.LogInformation(
                            "✅ 加载实体类型映射配置: 静态={Static}个, 学习={Learned}个, 统计={Stats}个",
                            config.StaticMappings.Count,
                            config.LearnedMappings.Count,
                            config.Statistics.Count);
                        */
                        return config;
                    }
                }

                // 配置文件不存在，创建默认配置并保存
                _logger.LogWarning("⚠️ 配置文件不存在: {Path}，创建默认配置并保存", _configPath);
                return CreateDefaultConfig();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ 加载配置失败: {Path}，使用默认配置", _configPath);
                return CreateDefaultConfig();
            }
        }

        /// <summary>
        /// 创建默认配置
        /// </summary>
        private EntityTypeMappingConfig CreateDefaultConfig()
        {
            var config = new EntityTypeMappingConfig
            {
                StaticMappings = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
                {
                    // ========== 基础实体类型 ==========
                    // 人物相关
                    { "人物", "PERSON" }, { "人名", "PERSON" }, { "姓名", "PERSON" },
                    { "作者", "PERSON" }, { "专家", "PERSON" }, { "用户", "PERSON" },
                    { "开发者", "PERSON" }, { "工程师", "PERSON" },

                    // 组织机构
                    { "组织", "ORG" }, { "机构", "ORG" }, { "公司", "ORG" },
                    { "企业", "ORG" }, { "团队", "ORG" }, { "部门", "ORG" },
                    { "学校", "ORG" }, { "大学", "ORG" },

                    // 地点位置
                    { "地点", "LOC" }, { "地名", "LOC" }, { "位置", "LOC" },
                    { "城市", "LOC" }, { "国家", "LOC" }, { "省份", "LOC" },

                    // ========== 技术领域 ==========
                    // 产品
                    { "产品", "PRODUCT" }, { "软件", "PRODUCT" }, { "系统", "PRODUCT" },
                    { "平台", "PRODUCT" }, { "工具", "PRODUCT" }, { "应用", "PRODUCT" },
                    { "框架", "PRODUCT" },

                    // 技术
                    { "技术", "TECH" }, { "算法", "TECH" }, { "协议", "TECH" },
                    { "标准", "TECH" }, { "方法", "TECH" }, { "架构", "TECH" },

                    // 编程语言
                    { "编程语言", "LANGUAGE" }, { "语言", "LANGUAGE" },

                    // 数据库
                    { "数据库", "DATABASE" },

                    // 模型
                    { "模型", "MODEL" }, { "大模型", "MODEL" }, { "AI模型", "MODEL" },

                    // ========== 版本和标识 ==========
                    { "版本", "VERSION" }, { "版本号", "VERSION" },
                    { "ID", "IDENTIFIER" }, { "标识符", "IDENTIFIER" },

                    // ========== 时间相关 ==========
                    { "日期", "DATE" }, { "时间", "DATE" }, { "年份", "DATE" },

                    // ========== 数值相关 ==========
                    { "数量", "NUMBER" }, { "数字", "NUMBER" },
                    { "金额", "MONEY" }, { "价格", "MONEY" },

                    // ========== 文档相关 ==========
                    { "文档", "DOCUMENT" }, { "文件", "DOCUMENT" },

                    // ========== 概念相关 ==========
                    { "概念", "CONCEPT" }, { "术语", "CONCEPT" },

                    // ========== 功能相关 ==========
                    { "功能", "FEATURE" }, { "特性", "FEATURE" }, { "能力", "FEATURE" },
                    { "模块", "MODULE" }, { "组件", "COMPONENT" }, { "插件", "PLUGIN" },

                    // ========== 其他 ==========
                    { "接口", "INTERFACE" }, { "API", "API" }, { "服务", "SERVICE" },
                    { "配置", "CONFIG" }, { "参数", "PARAMETER" }
                }
            };

            // 保存默认配置
            _ = Task.Run(() => SaveConfigAsync(config));

            return config;
        }

        /// <summary>
        /// 保存配置（异步）
        /// </summary>
        private async Task SaveConfigAsync(EntityTypeMappingConfig? config = null)
        {
            try
            {
                var configToSave = config ?? _config;

                lock (_lockObj)
                {
                    configToSave.LastUpdated = DateTime.UtcNow;
                }

                // 确保目录存在
                var directory = Path.GetDirectoryName(_configPath);
                if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                {
                    Directory.CreateDirectory(directory);
                }

                var options = new JsonSerializerOptions
                {
                    WriteIndented = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                };

                var json = JsonSerializer.Serialize(configToSave, options);
                await File.WriteAllTextAsync(_configPath, json);

                _lastSaveTime = DateTime.UtcNow;

                _logger.LogInformation(
                    "💾 保存实体类型映射配置: 静态={Static}个, 学习={Learned}个, 统计={Stats}个",
                    configToSave.StaticMappings.Count,
                    configToSave.LearnedMappings.Count,
                    configToSave.Statistics.Count);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存配置失败");
            }
        }

        /// <summary>
        /// 获取配置摘要
        /// </summary>
        public string GetConfigSummary()
        {
            lock (_lockObj)
            {
                var unmapped = GetUnmappedTypeStatistics();
                var topUnmapped = unmapped.Take(5)
                    .Select(x => $"{x.Type}({x.Frequency}次)")
                    .ToList();

                return $@"
📊 实体类型映射配置摘要
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
✅ 静态映射: {_config.StaticMappings.Count} 个
🎓 学习映射: {_config.LearnedMappings.Count} 个
📈 统计记录: {_config.Statistics.Count} 个
❓ 未映射类型: {unmapped.Count} 个

🔝 高频未映射类型 (Top 5):
{(topUnmapped.Count > 0 ? string.Join("\n", topUnmapped.Select((x, i) => $"   {i + 1}. {x}")) : "   (无)")}

⚙️ 自动学习: {(_config.AutoLearnEnabled ? "启用" : "禁用")}
📊 自动添加阈值: {_config.MinFrequencyForAutoAdd} 次
🕐 最后更新: {_config.LastUpdated:yyyy-MM-dd HH:mm:ss}
━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━
";
            }
        }
    }
}
