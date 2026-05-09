using StackExchange.Redis;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace ZSN.Cache.MemoryCache
{
    /// <summary>
    /// MemoryCacheService的列表操作和发布订阅部分
    /// </summary>
    public partial class MemoryCacheService
    {
        #region 列表操作

        /// <summary>
        /// 列表缓存项类
        /// </summary>
        private class ListEntry
        {
            public List<string> Items { get; }

            public ListEntry()
            {
                Items = new List<string>();
            }
        }

        /// <summary>
        /// 向列表左侧添加值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>添加后列表长度</returns>
        public long ListLeftPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            // 确保缓存项存在并且是ListEntry类型
            if (!_cache.TryGetValue(key, out var item))
            {
                var listItem = new ListEntry();
                var cacheItem = new MemoryCacheItem(key, listItem, TimeSpan.FromMinutes(_config.DefaultExpirationMinutes));
                
                if (_cache.TryAdd(key, cacheItem))
                {
                    // 更新内存使用统计
                    lock (_memoryUsageLock)
                    {
                        _currentMemoryUsage += cacheItem.Size;
                    }
                    
                    item = cacheItem;
                }
                else
                {
                    // 如果并发添加失败，则再次获取
                    if (!_cache.TryGetValue(key, out item))
                    {
                        return 0;
                    }
                }
            }
            
            // 检查缓存项是否过期
            if (item.IsExpired())
            {
                RemoveItem(key);
                return 0;
            }
            
            // 更新最后访问时间
            item.UpdateLastAccessTime();
            
            // 获取列表对象
            ListEntry listEntry;
            if (item.Value is ListEntry le)
            {
                listEntry = le;
            }
            else
            {
                // 如果缓存项不是列表，则创建一个新的列表
                listEntry = new ListEntry();
                item.Value = listEntry;
            }

            // 向列表左侧添加值
            listEntry.Items.Insert(0, value);
            
            // 更新内存使用统计
            lock (_memoryUsageLock)
            {
                _currentMemoryUsage += value.Length * 2;
            }
            
            return listEntry.Items.Count;
        }

        /// <summary>
        /// 向列表右侧添加值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="when">操作条件</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>添加后列表长度</returns>
        public long ListRightPush(string key, string value, When when = When.Always, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            // 确保缓存项存在并且是ListEntry类型
            if (!_cache.TryGetValue(key, out var item))
            {
                var listItem = new ListEntry();
                var cacheItem = new MemoryCacheItem(key, listItem, TimeSpan.FromMinutes(_config.DefaultExpirationMinutes));
                
                if (_cache.TryAdd(key, cacheItem))
                {
                    // 更新内存使用统计
                    lock (_memoryUsageLock)
                    {
                        _currentMemoryUsage += cacheItem.Size;
                    }
                    
                    item = cacheItem;
                }
                else
                {
                    // 如果并发添加失败，则再次获取
                    if (!_cache.TryGetValue(key, out item))
                    {
                        return 0;
                    }
                }
            }
            
            // 检查缓存项是否过期
            if (item.IsExpired())
            {
                RemoveItem(key);
                return 0;
            }
            
            // 更新最后访问时间
            item.UpdateLastAccessTime();
            
            // 获取列表对象
            ListEntry listEntry;
            if (item.Value is ListEntry le)
            {
                listEntry = le;
            }
            else
            {
                // 如果缓存项不是列表，则创建一个新的列表
                listEntry = new ListEntry();
                item.Value = listEntry;
            }

            // 向列表右侧添加值
            listEntry.Items.Add(value);
            
            // 更新内存使用统计
            lock (_memoryUsageLock)
            {
                _currentMemoryUsage += value.Length * 2;
            }
            
            return listEntry.Items.Count;
        }

        /// <summary>
        /// 从列表左侧弹出值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>弹出的值，列表为空时返回null</returns>
        public string? ListLeftPop(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // 检查是否过期
                if (item.IsExpired())
                {
                    RemoveItem(key);
                    return null;
                }
                
                // 更新最后访问时间
                item.UpdateLastAccessTime();
                
                // 获取列表对象
                if (item.Value is ListEntry listEntry && listEntry.Items.Count > 0)
                {
                    string value = listEntry.Items[0];
                    listEntry.Items.RemoveAt(0);
                    
                    // 如果列表为空，删除缓存项
                    if (listEntry.Items.Count == 0)
                    {
                        RemoveItem(key);
                    }
                    
                    return value;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 从列表右侧弹出值
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>弹出的值，列表为空时返回null</returns>
        public string? ListRightPop(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // 检查是否过期
                if (item.IsExpired())
                {
                    RemoveItem(key);
                    return null;
                }
                
                // 更新最后访问时间
                item.UpdateLastAccessTime();
                
                // 获取列表对象
                if (item.Value is ListEntry listEntry && listEntry.Items.Count > 0)
                {
                    string value = listEntry.Items[listEntry.Items.Count - 1];
                    listEntry.Items.RemoveAt(listEntry.Items.Count - 1);
                    
                    // 如果列表为空，删除缓存项
                    if (listEntry.Items.Count == 0)
                    {
                        RemoveItem(key);
                    }
                    
                    return value;
                }
            }
            
            return null;
        }

        /// <summary>
        /// 获取列表长度
        /// </summary>
        /// <param name="key">键</param>
        /// <param name="flags">命令标志</param>
        /// <param name="db">数据库索引</param>
        /// <returns>列表长度</returns>
        public long ListLength(string key, CommandFlags flags = CommandFlags.None, int? db = null)
        {
            if (_cache.TryGetValue(key, out var item))
            {
                // 检查是否过期
                if (item.IsExpired())
                {
                    RemoveItem(key);
                    return 0;
                }
                
                // 更新最后访问时间
                item.UpdateLastAccessTime();
                
                // 获取列表对象
                if (item.Value is ListEntry listEntry)
                {
                    return listEntry.Items.Count;
                }
            }
            
            return 0;
        }

        #endregion

        #region 发布订阅

        // 发布订阅事件处理程序存储
        private static readonly ConcurrentDictionary<string, List<Action<string, string>>> _subscribers = new ConcurrentDictionary<string, List<Action<string, string>>>();
        
        // 订阅通道锁对象
        private static readonly object _subscribersLock = new object();

        /// <summary>
        /// 发布消息
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>接收消息的订阅者数量</returns>
        public long Publish(string channel, string message, CommandFlags flags = CommandFlags.None)
        {
            if (_subscribers.TryGetValue(channel, out var handlers))
            {
                // 创建副本以避免枚举时修改集合
                var handlersCopy = new List<Action<string, string>>(handlers);
                
                // 通知所有订阅者
                foreach (var handler in handlersCopy)
                {
                    try
                    {
                        handler(channel, message);
                    }
                    catch (Exception ex)
                    {
                    }
                }
                
                return handlersCopy.Count;
            }
            
            return 0;
        }

        /// <summary>
        /// 异步发布消息
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="message">消息</param>
        /// <param name="flags">命令标志</param>
        /// <returns>接收消息的订阅者数量</returns>
        public async Task<long> PublishAsync(string channel, string message, CommandFlags flags = CommandFlags.None)
        {
            return await Task.FromResult(Publish(channel, message, flags));
        }

        /// <summary>
        /// 订阅频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="handler">消息处理器</param>
        /// <param name="flags">命令标志</param>
        public void Subscribe(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None)
        {
            lock (_subscribersLock)
            {
                if (!_subscribers.TryGetValue(channel, out var handlers))
                {
                    handlers = new List<Action<string, string>>();
                    _subscribers[channel] = handlers;
                }
                
                handlers.Add(handler);
            }
        }

        /// <summary>
        /// 异步订阅频道
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="handler">消息处理器</param>
        /// <param name="flags">命令标志</param>
        /// <returns>异步任务</returns>
        public async Task SubscribeAsync(string channel, Action<string, string> handler, CommandFlags flags = CommandFlags.None)
        {
            await Task.Run(() => Subscribe(channel, handler, flags));
        }

        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="flags">命令标志</param>
        public void Unsubscribe(string channel, CommandFlags flags = CommandFlags.None)
        {
            lock (_subscribersLock)
            {
                _subscribers.TryRemove(channel, out _);
            }
        }

        /// <summary>
        /// 异步取消订阅
        /// </summary>
        /// <param name="channel">频道</param>
        /// <param name="flags">命令标志</param>
        /// <returns>异步任务</returns>
        public async Task UnsubscribeAsync(string channel, CommandFlags flags = CommandFlags.None)
        {
            await Task.Run(() => Unsubscribe(channel, flags));
        }

        #endregion
    }
}
