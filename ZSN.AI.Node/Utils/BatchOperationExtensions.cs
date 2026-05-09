using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ZSN.AI.Node.Utils
{
    /// <summary>
    /// 批量操作扩展方法
    /// 用于优化数据库访问，减少查询次数
    /// </summary>
    public static class BatchOperationExtensions
    {
        /// <summary>
        /// 批量处理项目，自动分批以避免性能问题
        /// </summary>
        /// <typeparam name="T">项目类型</typeparam>
        /// <param name="items">要处理的项目列表</param>
        /// <param name="action">每个项目的处理操作</param>
        /// <param name="batchSize">每批的大小（默认100）</param>
        public static void ProcessInBatches<T>(this IEnumerable<T> items, Action<T> action, int batchSize = 100)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var batch = new List<T>(batchSize);
            foreach (var item in items)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    foreach (var batchItem in batch)
                    {
                        action(batchItem);
                    }
                    batch.Clear();
                }
            }

            // 处理剩余项目
            foreach (var batchItem in batch)
            {
                action(batchItem);
            }
        }

        /// <summary>
        /// 异步批量处理项目
        /// </summary>
        /// <typeparam name="T">项目类型</typeparam>
        /// <param name="items">要处理的项目列表</param>
        /// <param name="action">每个项目的异步处理操作</param>
        /// <param name="batchSize">每批的大小（默认100）</param>
        public static async Task ProcessInBatchesAsync<T>(this IEnumerable<T> items, Func<T, Task> action, int batchSize = 100)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var batch = new List<T>(batchSize);
            foreach (var item in items)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    foreach (var batchItem in batch)
                    {
                        await action(batchItem);
                    }
                    batch.Clear();
                }
            }

            // 处理剩余项目
            foreach (var batchItem in batch)
            {
                await action(batchItem);
            }
        }

        /// <summary>
        /// 分批处理项目并返回每批的结果
        /// </summary>
        /// <typeparam name="T">输入类型</typeparam>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="items">要处理的项目列表</param>
        /// <param name="action">每个批次的处理操作</param>
        /// <param name="batchSize">每批的大小（默认100）</param>
        public static IEnumerable<TResult> ProcessInBatches<T, TResult>(this IEnumerable<T> items, Func<IEnumerable<T>, IEnumerable<TResult>> action, int batchSize = 100)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var results = new List<TResult>();
            var batch = new List<T>(batchSize);

            foreach (var item in items)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    results.AddRange(action(batch));
                    batch.Clear();
                }
            }

            // 处理剩余项目
            if (batch.Count > 0)
            {
                results.AddRange(action(batch));
            }

            return results;
        }

        /// <summary>
        /// 异步分批处理项目并返回每批的结果
        /// </summary>
        /// <typeparam name="T">输入类型</typeparam>
        /// <typeparam name="TResult">结果类型</typeparam>
        /// <param name="items">要处理的项目列表</param>
        /// <param name="action">每个批次的异步处理操作</param>
        /// <param name="batchSize">每批的大小（默认100）</param>
        public static async Task<List<TResult>> ProcessInBatchesAsync<T, TResult>(this IEnumerable<T> items, Func<IEnumerable<T>, Task<List<TResult>>> action, int batchSize = 100)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var results = new List<TResult>();
            var batch = new List<T>(batchSize);

            foreach (var item in items)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    var batchResults = await action(batch);
                    results.AddRange(batchResults);
                    batch.Clear();
                }
            }

            // 处理剩余项目
            if (batch.Count > 0)
            {
                var batchResults = await action(batch);
                results.AddRange(batchResults);
            }

            return results;
        }

        /// <summary>
        /// 将列表分批
        /// </summary>
        /// <typeparam name="T">项目类型</typeparam>
        /// <param name="items">要分批的项目列表</param>
        /// <param name="batchSize">每批的大小</param>
        /// <returns>批次列表</returns>
        public static IEnumerable<List<T>> Batch<T>(this IEnumerable<T> items, int batchSize)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (batchSize <= 0) throw new ArgumentOutOfRangeException(nameof(batchSize), "批次大小必须大于0");

            var batch = new List<T>(batchSize);
            foreach (var item in items)
            {
                batch.Add(item);
                if (batch.Count >= batchSize)
                {
                    yield return batch;
                    batch = new List<T>(batchSize);
                }
            }

            // 返回最后一批
            if (batch.Count > 0)
            {
                yield return batch;
            }
        }

        /// <summary>
        /// 批量删除（使用 IN 子句优化）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">要删除的实体列表</param>
        /// <param name="deleteAction">批量删除操作</param>
        /// <param name="batchSize">每批大小（默认1000）</param>
        public static void BatchDelete<T>(this IEnumerable<T> entities, Action<List<T>> deleteAction, int batchSize = 1000)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (deleteAction == null) throw new ArgumentNullException(nameof(deleteAction));

            foreach (var batch in entities.Batch(batchSize))
            {
                deleteAction(batch.ToList());
            }
        }

        /// <summary>
        /// 批量更新（使用 CASE WHEN 优化）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">要更新的实体列表</param>
        /// <param name="updateAction">批量更新操作</param>
        /// <param name="batchSize">每批大小（默认500）</param>
        public static void BatchUpdate<T>(this IEnumerable<T> entities, Action<List<T>> updateAction, int batchSize = 500)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (updateAction == null) throw new ArgumentNullException(nameof(updateAction));

            foreach (var batch in entities.Batch(batchSize))
            {
                updateAction(batch.ToList());
            }
        }

        /// <summary>
        /// 批量插入（优化大量数据插入性能）
        /// </summary>
        /// <typeparam name="T">实体类型</typeparam>
        /// <param name="entities">要插入的实体列表</param>
        /// <param name="insertAction">批量插入操作</param>
        /// <param name="batchSize">每批大小（默认1000）</param>
        public static void BatchInsert<T>(this IEnumerable<T> entities, Action<List<T>> insertAction, int batchSize = 1000)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            if (insertAction == null) throw new ArgumentNullException(nameof(insertAction));

            foreach (var batch in entities.Batch(batchSize))
            {
                insertAction(batch.ToList());
            }
        }

        /// <summary>
        /// 智能批量操作 - 根据数据量自动选择最佳批次大小
        /// </summary>
        /// <typeparam name="T">项目类型</typeparam>
        /// <param name="items">要处理的项目列表</param>
        /// <param name="action">处理操作</param>
        public static void SmartBatch<T>(this IEnumerable<T> items, Action<List<T>> action)
        {
            if (items == null) throw new ArgumentNullException(nameof(items));
            if (action == null) throw new ArgumentNullException(nameof(action));

            var itemList = items.ToList();
            int count = itemList.Count;

            // 根据数据量自动选择批次大小
            int batchSize = count switch
            {
                <= 100 => count,              // 小数据量：一次性处理
                <= 1000 => 100,              // 中数据量：每批100
                <= 10000 => 500,             // 大数据量：每批500
                _ => 1000                    // 超大数据量：每批1000
            };

            itemList.Batch(batchSize).ToList().ForEach(batch => action(batch));
        }
    }
}
