using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ZSN.AI.DAL;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Repositories
{
    /// <summary>
    /// 向量存储库实现（使用 pgvector）
    /// </summary>
    public class VectorRepository : IVectorRepository
    {
        private readonly IConfiguration _configuration;
        private readonly ILogger<VectorRepository> _logger;
        private readonly string _connectionString;
        private bool _initialized = false;
        private readonly object _initLock = new object();

        public VectorRepository(
            IConfiguration configuration,
            ILogger<VectorRepository> logger)
        {
            _configuration = configuration;
            _logger = logger;

            // 从配置获取数据库连接
            var dbInfo = DbConfig.GetDbInfo("KnowledgeBaseDb");
            _connectionString = dbInfo.ConnectionString;
        }

        /// <summary>
        /// 初始化向量表和索引
        /// </summary>
        public async Task InitializeAsync(CancellationToken cancellationToken = default)
        {
            if (_initialized)
            {
                return;
            }

            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // 读取并执行初始化SQL脚本
                var sqlScript = await File.ReadAllTextAsync(
                    Path.Combine(AppContext.BaseDirectory, "Database/001_create_vector_tables.sql"),
                    cancellationToken);

                using var command = new NpgsqlCommand(sqlScript, connection);
                await command.ExecuteNonQueryAsync(cancellationToken);

                // 读取并执行唯一约束迁移脚本
                var migrationScript = await File.ReadAllTextAsync(
                    Path.Combine(AppContext.BaseDirectory, "Database/002_add_unique_constraints.sql"),
                    cancellationToken);

                using var migrationCommand = new NpgsqlCommand(migrationScript, connection);
                await migrationCommand.ExecuteNonQueryAsync(cancellationToken);

                // 读取并执行sequence_number字段迁移脚本
                var sequenceNumberScript = await File.ReadAllTextAsync(
                    Path.Combine(AppContext.BaseDirectory, "Database/003_add_sequence_number.sql"),
                    cancellationToken);

                using var sequenceNumberCommand = new NpgsqlCommand(sequenceNumberScript, connection);
                await sequenceNumberCommand.ExecuteNonQueryAsync(cancellationToken);

                _initialized = true;
                _logger.LogInformation("向量表初始化完成");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "初始化向量表时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 确保已初始化
        /// </summary>
        private void EnsureInitialized()
        {
            if (!_initialized)
            {
                // 自动初始化（线程安全）
                lock (_initLock)
                {
                    if (!_initialized)
                    {
                        // 使用同步方式等待异步初始化完成
                        InitializeAsync().GetAwaiter().GetResult();
                    }
                }
            }
        }

        /// <summary>
        /// 保存文档块向量
        /// </summary>
        public async Task SaveDocumentChunkAsync(DocumentChunkVector chunk, CancellationToken cancellationToken = default)
        {
            await SaveDocumentChunksAsync(new[] { chunk }, cancellationToken);
        }

        /// <summary>
        /// 批量保存文档块向量
        /// </summary>
        public async Task SaveDocumentChunksAsync(IEnumerable<DocumentChunkVector> chunks, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = @"
                    INSERT INTO document_chunks (document_id, chunk_id, content, embedding, metadata, token_count)
                    VALUES (@document_id, @chunk_id, @content, @embedding, @metadata::jsonb, @token_count)
                    ON CONFLICT (chunk_id) DO UPDATE
                        SET content = EXCLUDED.content,
                            embedding = EXCLUDED.embedding,
                            metadata = EXCLUDED.metadata,
                            token_count = EXCLUDED.token_count,
                            updated_at = CURRENT_TIMESTAMP";

                foreach (var chunk in chunks)
                {
                    using var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("document_id", chunk.DocumentId);
                    command.Parameters.AddWithValue("chunk_id", chunk.ChunkId);
                    command.Parameters.AddWithValue("content", chunk.Content);
                    command.Parameters.AddWithValue("embedding", chunk.Embedding ?? Array.Empty<float>());
                    command.Parameters.AddWithValue("metadata", chunk.Metadata != null ? JsonSerializer.Serialize(chunk.Metadata) : (string?)null);
                    command.Parameters.AddWithValue("token_count", chunk.TokenCount);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                _logger.LogDebug("成功保存 {Count} 个文档块向量", chunks.Count());
            }
            catch (Exception ex) {
                _logger.LogError(ex, "保存文档块向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 向量相似度搜索（文档块）
        /// </summary>
        public async Task<List<VectorSearchResult>> SearchDocumentChunksAsync(
            float[] queryEmbedding,
            int topK = 10,
            string? documentId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = new StringBuilder();
                sql.AppendLine("SELECT chunk_id, content, 1 - (embedding <=> @query_embedding::vector) as similarity");
                sql.AppendLine("FROM document_chunks");
                sql.AppendLine("WHERE embedding IS NOT NULL");

                if (!string.IsNullOrEmpty(documentId))
                {
                    sql.AppendLine("AND document_id = @document_id");
                }

                sql.AppendLine("ORDER BY embedding <=> @query_embedding::vector");
                sql.AppendLine("LIMIT @top_k");

                using var command = new NpgsqlCommand(sql.ToString(), connection);
                // Convert float array to vector string format for pgvector
                var vectorString = "[" + string.Join(",", queryEmbedding.Select(v => v.ToString("F6", System.Globalization.CultureInfo.InvariantCulture))) + "]";
                command.Parameters.AddWithValue("query_embedding", vectorString);
                command.Parameters.AddWithValue("top_k", topK);

                if (!string.IsNullOrEmpty(documentId))
                {
                    command.Parameters.AddWithValue("document_id", documentId);
                }

                var results = new List<VectorSearchResult>();
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new VectorSearchResult
                    {
                        Id = reader.GetString(0),
                        Content = reader.GetString(1),
                        Similarity = (float)reader.GetDouble(2)
                    });
                }

                _logger.LogDebug("向量搜索完成，返回 {Count} 个结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "向量搜索时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 向量相似度搜索（带上下文扩展）
        /// 返回匹配的chunk及其前后相邻的chunk，以提供完整的上下文
        /// </summary>
        public async Task<List<VectorSearchResultWithContext>> SearchDocumentChunksWithContextAsync(
            float[] queryEmbedding,
            int topK = 5,
            int contextChunks = 1, // 前后各多少个chunk作为上下文
            string? documentId = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // 第一步：获取最相似的topK个chunk
                var sql = new StringBuilder();
                sql.AppendLine("SELECT chunk_id, document_id, sequence_number, content, 1 - (embedding <=> @query_embedding::vector) as similarity");
                sql.AppendLine("FROM document_chunks");
                sql.AppendLine("WHERE embedding IS NOT NULL");

                if (!string.IsNullOrEmpty(documentId))
                {
                    sql.AppendLine("AND document_id = @document_id");
                }

                sql.AppendLine("ORDER BY embedding <=> @query_embedding::vector");
                sql.AppendLine("LIMIT @top_k");

                var vectorString = "[" + string.Join(",", queryEmbedding.Select(v => v.ToString("F6", System.Globalization.CultureInfo.InvariantCulture))) + "]";

                var matchedChunks = new List<(string chunkId, string documentId, int sequenceNumber, string content, float similarity)>();

                using (var command = new NpgsqlCommand(sql.ToString(), connection))
                {
                    command.Parameters.AddWithValue("query_embedding", vectorString);
                    command.Parameters.AddWithValue("top_k", topK);

                    if (!string.IsNullOrEmpty(documentId))
                    {
                        command.Parameters.AddWithValue("document_id", documentId);
                    }

                    using var reader = await command.ExecuteReaderAsync(cancellationToken);
                    while (await reader.ReadAsync(cancellationToken))
                    {
                        matchedChunks.Add((
                            reader.GetString(0),  // chunk_id
                            reader.GetString(1),  // document_id
                            reader.GetInt32(2),   // sequence_number
                            reader.GetString(3),  // content
                            (float)reader.GetDouble(4)  // similarity
                        ));
                    }
                }

                // 第二步：为每个匹配的chunk获取上下文chunk
                var results = new List<VectorSearchResultWithContext>();

                foreach (var (chunkId, docId, sequenceNumber, content, similarity) in matchedChunks)
                {
                    // 获取前后相邻的chunk
                    var contextSql = @"
                        SELECT chunk_id, sequence_number, content
                        FROM document_chunks
                        WHERE document_id = @doc_id
                          AND sequence_number BETWEEN @min_seq AND @max_seq
                          AND chunk_id != @chunk_id
                        ORDER BY sequence_number";

                    var adjacentChunks = new List<(string chunkId, int sequenceNumber, string content)>();

                    using (var contextCommand = new NpgsqlCommand(contextSql, connection))
                    {
                        contextCommand.Parameters.AddWithValue("doc_id", docId);
                        contextCommand.Parameters.AddWithValue("min_seq", sequenceNumber - contextChunks);
                        contextCommand.Parameters.AddWithValue("max_seq", sequenceNumber + contextChunks);
                        contextCommand.Parameters.AddWithValue("chunk_id", chunkId);

                        using var contextReader = await contextCommand.ExecuteReaderAsync(cancellationToken);
                        while (await contextReader.ReadAsync(cancellationToken))
                        {
                            adjacentChunks.Add((
                                contextReader.GetString(0),
                                contextReader.GetInt32(1),
                                contextReader.GetString(2)
                            ));
                        }
                    }

                    // 构建带上下文的结果
                    var result = new VectorSearchResultWithContext
                    {
                        ChunkId = chunkId,
                        Content = content,
                        Similarity = similarity,
                        SequenceNumber = sequenceNumber,
                        BeforeChunks = adjacentChunks
                            .Where(c => c.sequenceNumber < sequenceNumber)
                            .OrderBy(c => c.sequenceNumber)
                            .Select(c => c.content)
                            .ToList(),
                        AfterChunks = adjacentChunks
                            .Where(c => c.sequenceNumber > sequenceNumber)
                            .OrderBy(c => c.sequenceNumber)
                            .Select(c => c.content)
                            .ToList()
                    };

                    results.Add(result);
                }

                _logger.LogDebug("带上下文的向量搜索完成，返回 {Count} 个结果，每个结果包含 {Context} 个上下文chunk",
                    results.Count, contextChunks);

                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "带上下文的向量搜索时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 保存实体向量
        /// </summary>
        public async Task SaveEntityEmbeddingAsync(EntityEmbedding entity, CancellationToken cancellationToken = default)
        {
            await SaveEntityEmbeddingsAsync(new[] { entity }, cancellationToken);
        }

        /// <summary>
        /// 批量保存实体向量
        /// </summary>
        public async Task SaveEntityEmbeddingsAsync(IEnumerable<EntityEmbedding> entities, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = @"
                    INSERT INTO entity_embeddings (entity_id, entity_type, entity_text, embedding, metadata)
                    VALUES (@entity_id, @entity_type, @entity_text, @embedding, @metadata::jsonb)
                    ON CONFLICT (entity_id) DO UPDATE
                        SET entity_type = EXCLUDED.entity_type,
                            entity_text = EXCLUDED.entity_text,
                            embedding = EXCLUDED.embedding,
                            metadata = EXCLUDED.metadata,
                            updated_at = CURRENT_TIMESTAMP";

                foreach (var entity in entities)
                {
                    using var command = new NpgsqlCommand(sql, connection);
                    command.Parameters.AddWithValue("entity_id", entity.EntityId);
                    command.Parameters.AddWithValue("entity_type", entity.EntityType);
                    command.Parameters.AddWithValue("entity_text", entity.EntityText);
                    command.Parameters.AddWithValue("embedding", entity.Embedding ?? Array.Empty<float>());
                    command.Parameters.AddWithValue("metadata", entity.Metadata != null ? JsonSerializer.Serialize(entity.Metadata) : (string?)null);

                    await command.ExecuteNonQueryAsync(cancellationToken);
                }

                _logger.LogDebug("成功保存 {Count} 个实体向量", entities.Count());
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "保存实体向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 向量相似度搜索（实体）
        /// </summary>
        public async Task<List<VectorSearchResult>> SearchEntityEmbeddingsAsync(
            float[] queryEmbedding,
            int topK = 10,
            string? entityType = null,
            CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = new StringBuilder();
                sql.AppendLine("SELECT entity_id, entity_text, 1 - (embedding <=> @query_embedding::vector) as similarity");
                sql.AppendLine("FROM entity_embeddings");
                sql.AppendLine("WHERE embedding IS NOT NULL");

                if (!string.IsNullOrEmpty(entityType))
                {
                    sql.AppendLine("AND entity_type = @entity_type");
                }

                sql.AppendLine("ORDER BY embedding <=> @query_embedding::vector");
                sql.AppendLine("LIMIT @top_k");

                using var command = new NpgsqlCommand(sql.ToString(), connection);
                // Convert float array to vector string format for pgvector
                var vectorString = "[" + string.Join(",", queryEmbedding.Select(v => v.ToString("F6", System.Globalization.CultureInfo.InvariantCulture))) + "]";
                command.Parameters.AddWithValue("query_embedding", vectorString);
                command.Parameters.AddWithValue("top_k", topK);

                if (!string.IsNullOrEmpty(entityType))
                {
                    command.Parameters.AddWithValue("entity_type", entityType);
                }

                var results = new List<VectorSearchResult>();
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    results.Add(new VectorSearchResult
                    {
                        Id = reader.GetString(0),
                        Content = reader.GetString(1),
                        Similarity = (float)reader.GetDouble(2)
                    });
                }

                _logger.LogDebug("实体向量搜索完成，返回 {Count} 个结果", results.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "实体向量搜索时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 删除文档的所有向量
        /// </summary>
        public async Task DeleteDocumentAsync(string documentId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = "DELETE FROM document_chunks WHERE document_id = @document_id";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("document_id", documentId);

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogDebug("删除文档 {DocumentId} 的 {Count} 个向量", documentId, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除文档向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 删除实体向量
        /// </summary>
        public async Task DeleteEntityAsync(string entityId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = "DELETE FROM entity_embeddings WHERE entity_id = @entity_id";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("entity_id", entityId);

                await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogDebug("删除实体 {EntityId} 的向量", entityId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除实体向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 批量删除文档的所有向量
        /// </summary>
        public async Task DeleteDocumentsAsync(IEnumerable<string> documentIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var idList = documentIds.ToList();
                if (idList.Count == 0) return;

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = $"DELETE FROM document_chunks WHERE document_id = ANY(@document_ids)";
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("document_ids", idList.ToArray());

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogDebug("批量删除 {Count} 个文档的向量，共 {Rows} 行", idList.Count, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除文档向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 批量删除实体向量
        /// </summary>
        public async Task DeleteEntitiesAsync(IEnumerable<string> entityIds, CancellationToken cancellationToken = default)
        {
            try
            {
                var idList = entityIds.ToList();
                if (idList.Count == 0) return;

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = $"DELETE FROM entity_embeddings WHERE entity_id = ANY(@entity_ids)";
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("entity_ids", idList.ToArray());

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogDebug("批量删除 {Count} 个实体的向量，共 {Rows} 行", idList.Count, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "批量删除实体向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 删除知识库的所有文档向量
        /// </summary>
        public async Task DeleteKnowledgeBaseAsync(string knowledgeBaseId, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = "DELETE FROM document_chunks WHERE document_id LIKE @pattern";
                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("pattern", $"{knowledgeBaseId}%");

                var rowsAffected = await command.ExecuteNonQueryAsync(cancellationToken);
                _logger.LogDebug("删除知识库 {KnowledgeBaseId} 的所有向量，共 {Rows} 行", knowledgeBaseId, rowsAffected);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "删除知识库向量时发生错误: {Message}", ex.Message);
                throw;
            }
        }

        /// <summary>
        /// 获取文档块统计信息
        /// </summary>
        public async Task<int> GetDocumentChunkCountAsync(string? documentId = null, CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = "SELECT COUNT(*) FROM document_chunks";
                if (!string.IsNullOrEmpty(documentId))
                {
                    sql += " WHERE document_id = @document_id";
                }

                using var command = new NpgsqlCommand(sql, connection);
                if (!string.IsNullOrEmpty(documentId))
                {
                    command.Parameters.AddWithValue("document_id", documentId);
                }

                var count = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
                return (int)count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取文档块统计时发生错误: {Message}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 获取实体向量统计信息
        /// </summary>
        public async Task<int> GetEntityEmbeddingCountAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                var sql = "SELECT COUNT(*) FROM entity_embeddings";

                using var command = new NpgsqlCommand(sql, connection);
                var count = (long)(await command.ExecuteScalarAsync(cancellationToken) ?? 0);
                return (int)count;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "获取实体向量统计时发生错误: {Message}", ex.Message);
                return 0;
            }
        }

        /// <summary>
        /// 根据分块ID列表获取文档块
        /// </summary>
        public async Task<List<VectorSearchResult>> GetDocumentChunksByIdsAsync(
            List<string> chunkIds,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (chunkIds == null || chunkIds.Count == 0)
                {
                    _logger.LogWarning("获取文档块时，chunkIds为空");
                    return new List<VectorSearchResult>();
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // 使用ANY参数化查询避免SQL注入
                var sql = @"
                    SELECT chunk_id, content, metadata, document_id, sequence_number, token_count
                    FROM document_chunks
                    WHERE chunk_id = ANY(@chunk_ids)
                    ORDER BY sequence_number";

                using var command = new NpgsqlCommand(sql, connection);
                command.Parameters.AddWithValue("chunk_ids", chunkIds.ToArray());

                var results = new List<VectorSearchResult>();
                using var reader = await command.ExecuteReaderAsync(cancellationToken);

                while (await reader.ReadAsync(cancellationToken))
                {
                    var chunkId = reader.GetString(0);
                    var content = reader.GetString(1);
                    var metadata = reader.IsDBNull(2) ? null : reader.GetString(2);
                    var documentId = reader.GetString(3);
                    var sequenceNumber = reader.GetInt32(4);
                    var tokenCount = reader.GetInt32(5);

                    results.Add(new VectorSearchResult
                    {
                        Id = chunkId,
                        Content = content,
                        Similarity = 1.0f,  // 默认相似度，因为不是基于相似度查询
                        Metadata = new Dictionary<string, object>
                        {
                            { "document_id", documentId },
                            { "sequence_number", sequenceNumber },
                            { "token_count", tokenCount }
                        }
                    });

                    // 如果有额外的metadata，解析并添加
                    if (!string.IsNullOrEmpty(metadata))
                    {
                        try
                        {
                            var additionalMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
                            if (additionalMetadata != null)
                            {
                                foreach (var kvp in additionalMetadata)
                                {
                                    results[^1].Metadata.TryAdd(kvp.Key, kvp.Value);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "解析分块 {ChunkId} 的元数据失败", chunkId);
                        }
                    }
                }

                _logger.LogDebug("成功获取 {Count}/{Total} 个文档块", results.Count, chunkIds.Count);
                return results;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "根据ID获取文档块时发生错误: {Message}", ex.Message);
                return new List<VectorSearchResult>();
            }
        }

        /// <summary>
        /// 分页获取文档的所有分块
        /// </summary>
        public async Task<(List<VectorSearchResult> Chunks, int TotalCount)> GetDocumentChunksAsync(
            string documentId,
            int skip,
            int take,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrEmpty(documentId))
                {
                    _logger.LogWarning("获取文档块时，documentId为空");
                    return (new List<VectorSearchResult>(), 0);
                }

                using var connection = new NpgsqlConnection(_connectionString);
                await connection.OpenAsync(cancellationToken);

                // 首先获取总数
                var countSql = "SELECT COUNT(*) FROM document_chunks WHERE document_id = @document_id";
                using (var countCommand = new NpgsqlCommand(countSql, connection))
                {
                    countCommand.Parameters.AddWithValue("document_id", documentId);
                    var totalCount = (long)(await countCommand.ExecuteScalarAsync(cancellationToken) ?? 0);

                    if (totalCount == 0)
                    {
                        _logger.LogWarning("文档 {DocumentId} 没有找到分块数据", documentId);
                        return (new List<VectorSearchResult>(), 0);
                    }

                    // 获取分页数据
                    var dataSql = @"
                        SELECT chunk_id, content, metadata, document_id, sequence_number, token_count
                        FROM document_chunks
                        WHERE document_id = @document_id
                        ORDER BY sequence_number
                        LIMIT @take OFFSET @skip";

                    using var dataCommand = new NpgsqlCommand(dataSql, connection);
                    dataCommand.Parameters.AddWithValue("document_id", documentId);
                    dataCommand.Parameters.AddWithValue("take", take);
                    dataCommand.Parameters.AddWithValue("skip", skip);

                    var results = new List<VectorSearchResult>();
                    using var reader = await dataCommand.ExecuteReaderAsync(cancellationToken);

                    while (await reader.ReadAsync(cancellationToken))
                    {
                        var chunkId = reader.GetString(0);
                        var content = reader.GetString(1);
                        var metadata = reader.IsDBNull(2) ? null : reader.GetString(2);
                        var docId = reader.GetString(3);
                        var sequenceNumber = reader.GetInt32(4);
                        var tokenCount = reader.GetInt32(5);

                        results.Add(new VectorSearchResult
                        {
                            Id = chunkId,
                            Content = content,
                            Similarity = 1.0f,
                            Metadata = new Dictionary<string, object>
                            {
                                { "document_id", docId },
                                { "sequence_number", sequenceNumber },
                                { "token_count", tokenCount }
                            }
                        });

                        // 如果有额外的metadata，解析并添加
                        if (!string.IsNullOrEmpty(metadata))
                        {
                            try
                            {
                                var additionalMetadata = JsonSerializer.Deserialize<Dictionary<string, object>>(metadata);
                                if (additionalMetadata != null)
                                {
                                    foreach (var kvp in additionalMetadata)
                                    {
                                        results[^1].Metadata.TryAdd(kvp.Key, kvp.Value);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogWarning(ex, "解析分块 {ChunkId} 的元数据失败", chunkId);
                            }
                        }
                    }

                    _logger.LogDebug("成功获取文档 {DocumentId} 的 {Count}/{Total} 个分块（跳过{Skip}，取{Take}）",
                        documentId, results.Count, totalCount, skip, take);

                    return (results, (int)totalCount);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "分页获取文档块时发生错误: {Message}", ex.Message);
                return (new List<VectorSearchResult>(), 0);
            }
        }
    }
}
