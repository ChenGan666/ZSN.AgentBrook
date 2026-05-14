using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Npgsql;
using ZSN.AI.DAL;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.AI.KnowledgeBase.Interface;
using ZSN.AI.KnowledgeBase.Models;

namespace ZSN.AI.KnowledgeBase.Repositories
{
    public class ImageRepository : IImageRepository
    {
        private readonly string _connectionString;
        private readonly ILogger<ImageRepository> _logger;

        public ImageRepository(IConfiguration configuration, ILogger<ImageRepository> logger)
        {
            var dbInfo = DbConfig.GetDbInfo("KnowledgeBaseDb");
            _connectionString = dbInfo.ConnectionString;
            _logger = logger;
        }

        public async Task SaveImageInfosAsync(List<DocumentImageInfo> images, CancellationToken ct = default)
        {
            if (images == null || images.Count == 0) return;

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var sb = new StringBuilder();
            for (int i = 0; i < images.Count; i++)
            {
                var img = images[i];
                var metadata = img.Metadata != null ? JsonSerializer.Serialize(img.Metadata) : "null";
                var desc = EscapeSql(img.Description);
                var ocr = EscapeSql(img.OcrText);
                var fname = EscapeSql(img.OriginalFilename);
                var mime = EscapeSql(img.MimeType);

                if (i > 0) sb.Append(';');
                sb.Append($@"INSERT INTO document_images (document_id, image_id, page_number, sequence_number,
                    original_filename, storage_path, storage_type, mime_type, file_size, width, height,
                    content_hash, description, ocr_text, description_status, is_decorative, metadata)
                    VALUES ('{EscapeSql(img.DocumentId)}', '{EscapeSql(img.ImageId)}',
                    {(img.PageNumber.HasValue ? img.PageNumber.Value.ToString() : "NULL")},
                    {img.SequenceNumber},
                    {(fname != null ? $"'{fname}'" : "NULL")},
                    '{EscapeSql(img.StoragePath)}', '{EscapeSql(img.StorageType)}',
                    {(mime != null ? $"'{mime}'" : "NULL")},
                    {(img.FileSize.HasValue ? img.FileSize.Value.ToString() : "NULL")},
                    {(img.Width.HasValue ? img.Width.Value.ToString() : "NULL")},
                    {(img.Height.HasValue ? img.Height.Value.ToString() : "NULL")},
                    {(img.ContentHash != null ? $"'{EscapeSql(img.ContentHash)}'" : "NULL")},
                    {(desc != null ? $"'{desc}'" : "NULL")},
                    {(ocr != null ? $"'{ocr}'" : "NULL")},
                    '{EscapeSql(img.DescriptionStatus)}',
                    {(img.IsDecorative ? "TRUE" : "FALSE")},
                    {(metadata != "null" ? $"'{EscapeSql(metadata)}'::jsonb" : "NULL")})
                    ON CONFLICT (image_id) DO NOTHING");
            }

            using var cmd = new NpgsqlCommand(sb.ToString(), conn);
            cmd.CommandTimeout = 60;
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task SaveChunkImageRelationsAsync(List<ChunkImageRelation> relations, CancellationToken ct = default)
        {
            if (relations == null || relations.Count == 0) return;

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var sb = new StringBuilder();
            for (int i = 0; i < relations.Count; i++)
            {
                var r = relations[i];
                if (i > 0) sb.Append(';');
                sb.Append($@"INSERT INTO chunk_image_relations (chunk_id, image_id, relation_type)
                    VALUES ('{EscapeSql(r.ChunkId)}', '{EscapeSql(r.ImageId)}', '{EscapeSql(r.RelationType)}')
                    ON CONFLICT (chunk_id, image_id) DO NOTHING");
            }

            using var cmd = new NpgsqlCommand(sb.ToString(), conn);
            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<List<string>> GetExistingHashesAsync(List<string> hashes, CancellationToken ct = default)
        {
            if (hashes == null || hashes.Count == 0) return new List<string>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var result = new List<string>();
            var batchSize = 500;
            for (int i = 0; i < hashes.Count; i += batchSize)
            {
                var batch = hashes.Skip(i).Take(batchSize).ToList();
                using var cmd = new NpgsqlCommand(
                    "SELECT content_hash FROM document_images WHERE content_hash = ANY(@hashes)", conn);
                cmd.Parameters.AddWithValue("hashes", batch.ToArray());
                using var reader = await cmd.ExecuteReaderAsync(ct);
                while (await reader.ReadAsync(ct))
                {
                    result.Add(reader.GetString(0));
                }
            }
            return result;
        }

        public async Task<List<DocumentImageInfo>> GetByDocumentIdAsync(string documentId, CancellationToken ct = default)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new NpgsqlCommand(
                "SELECT * FROM document_images WHERE document_id = @docId ORDER BY sequence_number", conn);
            cmd.Parameters.AddWithValue("docId", documentId);

            var result = new List<DocumentImageInfo>();
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(MapReaderToImageInfo(reader));
            }
            return result;
        }

        public async Task<Dictionary<string, List<ImageSearchResult>>> GetImagesByChunkIdsAsync(
            List<string> chunkIds, CancellationToken ct = default)
        {
            if (chunkIds == null || chunkIds.Count == 0) return new Dictionary<string, List<ImageSearchResult>>();

            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new NpgsqlCommand(
                @"SELECT di.*, cir.chunk_id
                FROM document_images di
                JOIN chunk_image_relations cir ON di.image_id = cir.image_id
                WHERE cir.chunk_id = ANY(@chunkIds)", conn);
            cmd.Parameters.AddWithValue("chunkIds", chunkIds.ToArray());

            var result = new Dictionary<string, List<ImageSearchResult>>();
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                var chunkId = reader.GetString(reader.GetOrdinal("chunk_id"));
                var img = MapReaderToSearchResult(reader);

                if (!result.ContainsKey(chunkId))
                    result[chunkId] = new List<ImageSearchResult>();
                result[chunkId].Add(img);
            }
            return result;
        }

        public async Task DeleteByDocumentIdAsync(string documentId, CancellationToken ct = default)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var tx = await conn.BeginTransactionAsync(ct);
            try
            {
                // 先删除关联记录
                using (var cmd1 = new NpgsqlCommand(
                    @"DELETE FROM chunk_image_relations
                    WHERE image_id IN (SELECT image_id FROM document_images WHERE document_id = @docId)", conn, tx))
                {
                    cmd1.Parameters.AddWithValue("docId", documentId);
                    await cmd1.ExecuteNonQueryAsync(ct);
                }

                // 再删除图片记录
                using (var cmd2 = new NpgsqlCommand(
                    "DELETE FROM document_images WHERE document_id = @docId", conn, tx))
                {
                    cmd2.Parameters.AddWithValue("docId", documentId);
                    await cmd2.ExecuteNonQueryAsync(ct);
                }

                await tx.CommitAsync(ct);
            }
            catch
            {
                await tx.RollbackAsync(ct);
                throw;
            }
        }

        public async Task<List<DocumentImageInfo>> GetFailedDescriptionsAsync(
            string? documentId = null, CancellationToken ct = default)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            var sql = "SELECT * FROM document_images WHERE description_status = 'failed'";
            if (!string.IsNullOrEmpty(documentId))
                sql += " AND document_id = @docId";

            using var cmd = new NpgsqlCommand(sql, conn);
            if (!string.IsNullOrEmpty(documentId))
                cmd.Parameters.AddWithValue("docId", documentId);

            var result = new List<DocumentImageInfo>();
            using var reader = await cmd.ExecuteReaderAsync(ct);
            while (await reader.ReadAsync(ct))
            {
                result.Add(MapReaderToImageInfo(reader));
            }
            return result;
        }

        public async Task UpdateDescriptionAsync(
            string imageId, string description, string? ocrText, string status,
            CancellationToken ct = default)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new NpgsqlCommand(
                @"UPDATE document_images SET description = @desc, ocr_text = @ocr,
                    description_status = @status, updated_at = NOW()
                WHERE image_id = @imageId", conn);
            cmd.Parameters.AddWithValue("desc", description ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("ocr", ocrText ?? (object)DBNull.Value);
            cmd.Parameters.AddWithValue("status", status);
            cmd.Parameters.AddWithValue("imageId", imageId);

            await cmd.ExecuteNonQueryAsync(ct);
        }

        public async Task<DocumentImageInfo?> GetByImageIdAsync(string imageId, CancellationToken ct = default)
        {
            using var conn = new NpgsqlConnection(_connectionString);
            await conn.OpenAsync(ct);

            using var cmd = new NpgsqlCommand(
                "SELECT * FROM document_images WHERE image_id = @imageId", conn);
            cmd.Parameters.AddWithValue("imageId", imageId);

            using var reader = await cmd.ExecuteReaderAsync(ct);
            if (await reader.ReadAsync(ct))
            {
                return MapReaderToImageInfo(reader);
            }
            return null;
        }

        private DocumentImageInfo MapReaderToImageInfo(NpgsqlDataReader reader)
        {
            return new DocumentImageInfo
            {
                Id = reader.GetInt64(reader.GetOrdinal("id")),
                DocumentId = reader.GetString(reader.GetOrdinal("document_id")),
                ImageId = reader.GetString(reader.GetOrdinal("image_id")),
                PageNumber = reader.IsDBNull(reader.GetOrdinal("page_number"))
                    ? null : reader.GetInt32(reader.GetOrdinal("page_number")),
                SequenceNumber = reader.GetInt32(reader.GetOrdinal("sequence_number")),
                OriginalFilename = reader.IsDBNull(reader.GetOrdinal("original_filename"))
                    ? null : reader.GetString(reader.GetOrdinal("original_filename")),
                StoragePath = reader.GetString(reader.GetOrdinal("storage_path")),
                StorageType = reader.GetString(reader.GetOrdinal("storage_type")),
                MimeType = reader.IsDBNull(reader.GetOrdinal("mime_type"))
                    ? null : reader.GetString(reader.GetOrdinal("mime_type")),
                FileSize = reader.IsDBNull(reader.GetOrdinal("file_size"))
                    ? null : reader.GetInt64(reader.GetOrdinal("file_size")),
                Width = reader.IsDBNull(reader.GetOrdinal("width"))
                    ? null : reader.GetInt32(reader.GetOrdinal("width")),
                Height = reader.IsDBNull(reader.GetOrdinal("height"))
                    ? null : reader.GetInt32(reader.GetOrdinal("height")),
                ContentHash = reader.IsDBNull(reader.GetOrdinal("content_hash"))
                    ? null : reader.GetString(reader.GetOrdinal("content_hash")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null : reader.GetString(reader.GetOrdinal("description")),
                OcrText = reader.IsDBNull(reader.GetOrdinal("ocr_text"))
                    ? null : reader.GetString(reader.GetOrdinal("ocr_text")),
                DescriptionStatus = reader.GetString(reader.GetOrdinal("description_status")),
                IsDecorative = reader.GetBoolean(reader.GetOrdinal("is_decorative")),
                Metadata = reader.IsDBNull(reader.GetOrdinal("metadata"))
                    ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(
                        reader.GetString(reader.GetOrdinal("metadata"))),
                CreatedAt = reader.GetDateTime(reader.GetOrdinal("created_at")),
                UpdatedAt = reader.GetDateTime(reader.GetOrdinal("updated_at"))
            };
        }

        private ImageSearchResult MapReaderToSearchResult(NpgsqlDataReader reader)
        {
            return new ImageSearchResult
            {
                ImageId = reader.GetString(reader.GetOrdinal("image_id")),
                DocumentId = reader.GetString(reader.GetOrdinal("document_id")),
                StoragePath = reader.GetString(reader.GetOrdinal("storage_path")),
                StorageType = reader.GetString(reader.GetOrdinal("storage_type")),
                MimeType = reader.IsDBNull(reader.GetOrdinal("mime_type"))
                    ? null : reader.GetString(reader.GetOrdinal("mime_type")),
                Description = reader.IsDBNull(reader.GetOrdinal("description"))
                    ? null : reader.GetString(reader.GetOrdinal("description")),
                OcrText = reader.IsDBNull(reader.GetOrdinal("ocr_text"))
                    ? null : reader.GetString(reader.GetOrdinal("ocr_text")),
                PageNumber = reader.IsDBNull(reader.GetOrdinal("page_number"))
                    ? null : reader.GetInt32(reader.GetOrdinal("page_number")),
                MatchType = "text",
                Metadata = reader.IsDBNull(reader.GetOrdinal("metadata"))
                    ? null : JsonSerializer.Deserialize<Dictionary<string, object>>(
                        reader.GetString(reader.GetOrdinal("metadata")))
            };
        }

        private static string? EscapeSql(string? input)
        {
            if (input == null) return null;
            return input.Replace("'", "''");
        }
    }
}
