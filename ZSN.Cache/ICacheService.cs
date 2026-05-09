using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZSN.Cache
{
    /// <summary>
    /// 缓存服务通用接口，定义所有缓存操作
    /// </summary>
    public interface ICacheService
    {
        #region 字符串操作

        /// <summary>
        /// 设置字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool StringSet(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步设置字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        Task<bool> StringSetAsync(string key, string value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字符串值，不存在时返回null</returns>
        string? StringGet(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步获取字符串值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字符串值，不存在时返回null</returns>
        Task<string?> StringGetAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        #endregion

        #region 对象操作

        /// <summary>
        /// 设置对象（序列化为JSON）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">对象</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool Set<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步设置对象（序列化为JSON）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="value">对象</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        Task<bool> SetAsync<T>(string key, T value, TimeSpan? expiry = null, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 获取对象（从JSON反序列化）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象，不存在时返回默认值</returns>
        T? Get<T>(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步获取对象（从JSON反序列化）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象，不存在时返回默认值</returns>
        Task<T?> GetAsync<T>(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        #endregion

        #region 键操作

        /// <summary>
        /// 判断键是否存在
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否存在</returns>
        bool KeyExists(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步判断键是否存在
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否存在</returns>
        Task<bool> KeyExistsAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 删除键
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否删除成功</returns>
        bool KeyDelete(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步删除键
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否删除成功</returns>
        Task<bool> KeyDeleteAsync(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 设置键过期时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否设置成功</returns>
        bool KeyExpire(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步设置键过期时间
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="expiry">过期时间</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否设置成功</returns>
        Task<bool> KeyExpireAsync(string key, TimeSpan? expiry, CommandFlags flags = CommandFlags.None, int? db = null);

        #endregion

        #region 哈希操作

        /// <summary>
        /// 设置哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功（字段是新增时返回true，更新时返回false）</returns>
        bool HashSet(string key, string hashField, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步设置哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功（字段是新增时返回true，更新时返回false）</returns>
        Task<bool> HashSetAsync(string key, string hashField, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 设置哈希对象字段（序列化为JSON）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="value">对象</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功</returns>
        bool HashSet<T>(string key, string hashField, T value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 获取哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字段值，不存在时返回null</returns>
        string? HashGet(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 异步获取哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字段值，不存在时返回null</returns>
        Task<string?> HashGetAsync(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 获取哈希对象字段（从JSON反序列化）
        /// </summary>
        /// <typeparam name="T">对象类型</typeparam>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>对象，不存在时返回默认值</returns>
        T? HashGet<T>(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 删除哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="hashField">哈希字段</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>是否成功删除</returns>
        bool HashDelete(string key, string hashField, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 获取所有哈希字段
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>字段名和值的字典</returns>
        Dictionary<string, string> HashGetAll(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        #endregion

        #region 列表操作

        /// <summary>
        /// 向列表左侧添加值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>添加后列表长度</returns>
        long ListLeftPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 向列表右侧添加值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>添加后列表长度</returns>
        long ListRightPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 从列表左侧弹出值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>弹出的值，列表为空时返回null</returns>
        string? ListLeftPop(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 从列表右侧弹出值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>弹出的值，列表为空时返回null</returns>
        string? ListRightPop(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>列表长度</returns>
        long ListLength(string key, CommandFlags flags = CommandFlags.None, int? db = null);

        #endregion

        #region 计数器

        /// <summary>
        /// 递增
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">递增值，默认为1</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>递增后的值</returns>
        long Increment(string key, long value = 1, CommandFlags flags = CommandFlags.None, int? db = null);

        /// <summary>
        /// 递减
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">递减值，默认为1</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>递减后的值</returns>
        long Decrement(string key, long value = 1, CommandFlags flags = CommandFlags.None, int? db = null);

        #endregion

        #region 发布订阅

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>接收消息的订阅者数量</returns>
        long Publish(string channel, string message, CommandFlags flags = CommandFlags.None);

        /// <summary>
        /// 异步发布消息
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>接收消息的订阅者数量</returns>
        Task<long> PublishAsync(string channel, string message, CommandFlags flags = CommandFlags.None);

        /// <summary>
        /// 订阅频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="handler">消息处理器</param>
        /// <param name="flags">命令标志</param>
        void Subscribe(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None);

        /// <summary>
        /// 异步订阅频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="handler">消息处理器</param>
        /// <param name="flags">命令标志</param>
        /// <returns>异步任务</returns>
        Task SubscribeAsync(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None);

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="flags">命令标志</param>
        void Unsubscribe(string channel, CommandFlags flags = CommandFlags.None);

        /// <summary>
        /// 异步取消订阅
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="flags">命令标志</param>
        /// <returns>异步任务</returns>
        Task UnsubscribeAsync(string channel, CommandFlags flags = CommandFlags.None);

        #endregion
    }
}
