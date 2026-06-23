using System;
using System.Collections.Generic;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using Garnet.client;

namespace ZSN.Utils.Core.Helpers
{
    public class GarnetHelper : IDisposable
    {
        private static readonly string ConnectionString = "127.0.0.1:6379";
        private static GarnetClient _client;

        static GarnetHelper()
        {
            ConnectionString = ConfigHelper.GetString("GarnetConnectionString");
            if (ConnectionString == "")
            {
                ConsoleLogHelper.WriteLine($"配置文件中缺失 GarnetConnectionString", ConsoleColor.Red);
            }
            else
            {
                IPEndPoint endPoint = IPEndPoint.Parse(ConnectionString);

                _client = new GarnetClient(endPoint.Address.ToString(), endPoint.Port);
            }
        }

        /// <summary>
        /// 设置字符串类型的键值对
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> SetStringAsync(string key, string value)
        {
            try
            {
                return await _client.StringSetAsync(key, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置字符串失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取字符串类型的键值
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>字符串值，如果不存在则返回 null</returns>
        public static async Task<string> GetStringAsync(string key)
        {
            try
            {
                return await _client.StringGetAsync(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取字符串失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 设置对象值，序列化为 JSON 格式
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">要存储的对象</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> SetObjectAsync<T>(string key, T value)
        {
            try
            {
                var json = JsonSerializer.Serialize(value);
                return await _client.StringSetAsync(key, json);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"设置对象失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取对象值，反序列化为指定类型
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <returns>反序列化的对象，如果不存在则返回默认值</returns>
        public static async Task<T> GetObjectAsync<T>(string key)
        {
            try
            {
                var value = await _client.StringGetAsync(key);
                return value != null ? JsonSerializer.Deserialize<T>(value) : default;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取对象失败: {ex.Message}");
                return default;
            }
        }

        /// <summary>
        /// 在列表中添加值
        /// </summary>
        /// <param name="key">列表键</param>
        /// <param name="value">要添加的值</param>
        /// <returns>列表的长度</returns>
        public static async Task<long> AddToListAsync(string key, string value)
        {
            try
            {
                return await _client.ListLeftPushAsync(key, value);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"添加到列表失败: {ex.Message}");
                return -1;
            }
        }

        /// <summary>
        /// 获取列表中指定范围的值
        /// </summary>
        /// <param name="key">列表键</param>
        /// <param name="start">起始索引，默认值为 0</param>
        /// <param name="stop">结束索引，默认值为 -1（表示到列表的最后一个元素）</param>
        /// <returns>包含指定范围内的所有值的列表</returns>
        public static async Task<List<string>> GetListAsync(string key, int start = 0, int stop = -1)
        {
            try
            {
                var values = await _client.ListRangeAsync(key, start, stop);
                return values != null ? new List<string>(values) : new List<string>();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"获取列表范围失败: {ex.Message}");
                return null;
            }
        }

        

        /// <summary>
        /// 删除键
        /// </summary>
        /// <param name="key">键</param>
        /// <returns>操作是否成功</returns>
        public static async Task<bool> DeleteKeyAsync(string key)
        {
            try
            {
                return await _client.KeyDeleteAsync(key);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"删除键失败: {ex.Message}");
                return false;
            }
        }


        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            _client?.Dispose();
        }
    }
}
