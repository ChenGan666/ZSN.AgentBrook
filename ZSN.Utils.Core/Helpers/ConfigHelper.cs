using ZSN.Utils.Core.DI;
using ZSN.Utils.Core.Extensions;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Text.Json.Nodes;
using System.Text.Json;
using System;

namespace ZSN.Utils.Core.Helpers
{
    /// <summary>
    /// 配置文件读取工具类
    /// </summary>
    public class ConfigHelper
    {
        private static readonly string ConfigFilePath = "appsettings.json";
        public static IConfiguration Configuration =>
            ServiceLocator.GetInstance<IConfiguration>()
            ?? new ConfigurationBuilder().AddJsonFile(ConfigFilePath).Build();


        public static IConfigurationSection GetSection(string key)
        {
            return Configuration.GetSection(key);
        }

        public static string GetConfigurationValue(string key)
        {
            // 支持嵌套配置项，例如 "PromptTemplates:ClawAITaskPlanningPrompt"
            if (string.IsNullOrEmpty(key))
                return null;
            
            // 如果包含冒号，使用GetSection方法处理嵌套路径
            if (key.Contains(':'))
            {
                var keys = key.Split(':');
                IConfigurationSection section = Configuration.GetSection(keys[0]);
                for (int i = 1; i < keys.Length; i++)
                {
                    if (section == null)
                        return null;
                    section = section.GetSection(keys[i]);
                }
                return section?.Value;
            }
            
            // 简单Key直接访问
            return Configuration[key];
        }

        public static string GetConfigurationValue(string section, string key)
        {
            return GetSection(section)?[key];
        }

        public static string GetConnectionString(string key)
        {
            string _conn = Configuration.GetConnectionString(key);
            return _conn;
        }

        #region GetString 获取配置字符串值

        /// <summary>
        ///     获取配置字符串值
        /// </summary>
        /// <param name="configStr">配置名称</param>
        /// <param name="defaultStr">没有配置项时返回的字符串</param>
        /// <returns>字符串值</returns>
        public static string GetString(string configStr, string defaultStr = "")
        {
            var result = GetConfigurationValue(configStr);
            if (result == null)
                result = defaultStr;
            return result;
        }

        #endregion

        #region GetInt 获取配置整数值，无值返回 -1

        /// <summary>
        ///     获取配置整数值，无值返回 -1
        /// </summary>
        /// <param name="configStr">配置名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>整数值</returns>
        public static int GetInt(string configStr, int defaultValue = -1)
        {
            return GetString(configStr).ToInt32(defaultValue);
        }

        #endregion

        #region GetDecimal 获取配置浮点值

        /// <summary>
        ///     获取配置浮点值
        /// </summary>
        /// <param name="configStr">配置名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>浮点值</returns>
        public static decimal GetDecimal(string configStr, decimal defaultValue = -1)
        {
            return GetString(configStr).ToDecimal(defaultValue);
        }

        #endregion

        #region GetBool 获取配置布尔值

        /// <summary>
        ///     获取配置布尔值(1或true为真，不区分大小写)
        /// </summary>
        /// <param name="configStr">配置名称</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns>布尔值</returns>
        public static bool GetBool(string configStr, bool defaultValue = false)
        {
            return GetString(configStr).ToBoolean(defaultValue);
        }

        #endregion

        /// <summary>
        /// 更新并保存配置值（支持简单Key，复杂结构可以扩展）
        /// </summary>
        /// <param name="key">配置的Key，例如 "Logging:LogLevel:Default"</param>
        /// <param name="value">要保存的值</param>
        public static void SetConfigurationValue(string key, string value)
        {
            if (!File.Exists(ConfigFilePath))
                throw new FileNotFoundException("配置文件不存在", ConfigFilePath);

            var jsonText = File.ReadAllText(ConfigFilePath);
            var jsonObject = JsonNode.Parse(jsonText) as JsonObject;
            if (jsonObject == null)
                throw new Exception("配置文件格式错误");

            // 支持嵌套Key，例如 "Logging:LogLevel:Default"
            var keys = key.Split(':');
            JsonObject current = jsonObject;
            for (int i = 0; i < keys.Length - 1; i++)
            {
                if (!current.ContainsKey(keys[i]) || current[keys[i]] == null)
                {
                    current[keys[i]] = new JsonObject();
                }
                current = current[keys[i]]!.AsObject();
            }

            current[keys[^1]] = value;

            // 保存回文件，格式化写入
            var options = new JsonSerializerOptions { WriteIndented = true };
            File.WriteAllText(ConfigFilePath, jsonObject.ToJsonString(options));
        }
    }
}
