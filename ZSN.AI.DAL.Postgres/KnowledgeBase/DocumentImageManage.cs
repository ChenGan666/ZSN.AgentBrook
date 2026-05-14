using System.Data;
using System.Text.Json;
using Npgsql;
using NpgsqlTypes;
using ZSN.AI.DAL;
using ZSN.AI.Entity.KnowledgeBase;
using ZSN.Utils.Core.Data;

namespace ZSN.AI.DAL.Postgres
{
    public class DocumentImageManage : IDocumentImageManage
    {
        private string ConnectionName = "KnowledgeBaseDb";

        public string SetConnectionName(string connName)
        {
            return ConnectionName = connName;
        }

        public DocumentImageInfo DocumentImage_GetByImageId(string imageId)
        {
            string sql = "SELECT * FROM document_images WHERE image_id = @imageId";
            var param = new NpgsqlParameter("@imageId", NpgsqlDbType.Text) { Value = imageId };
            DataSet ds = DbHelper.ExecuteDataset(DbConfig.GetDbInfo(ConnectionName), CommandType.Text, sql, new System.Data.Common.DbParameter[] { param });

            if (ds.Tables.Count > 0 && ds.Tables[0].Rows.Count > 0)
            {
                return DocumentImage_DataRowToModel(ds.Tables[0].Rows[0]);
            }
            return null;
        }

        public DocumentImageInfo DocumentImage_DataRowToModel(DataRow row)
        {
            if (row == null) return null;

            var model = new DocumentImageInfo();
            if (row["id"] != null && row["id"] != DBNull.Value)
                model.Id = Convert.ToInt64(row["id"]);
            if (row["document_id"] != null && row["document_id"] != DBNull.Value)
                model.DocumentId = row["document_id"].ToString();
            if (row["image_id"] != null && row["image_id"] != DBNull.Value)
                model.ImageId = row["image_id"].ToString();
            if (row["page_number"] != null && row["page_number"] != DBNull.Value)
                model.PageNumber = Convert.ToInt32(row["page_number"]);
            if (row["sequence_number"] != null && row["sequence_number"] != DBNull.Value)
                model.SequenceNumber = Convert.ToInt32(row["sequence_number"]);
            if (row["original_filename"] != null && row["original_filename"] != DBNull.Value)
                model.OriginalFilename = row["original_filename"].ToString();
            if (row["storage_path"] != null && row["storage_path"] != DBNull.Value)
                model.StoragePath = row["storage_path"].ToString();
            if (row["storage_type"] != null && row["storage_type"] != DBNull.Value)
                model.StorageType = row["storage_type"].ToString();
            if (row["mime_type"] != null && row["mime_type"] != DBNull.Value)
                model.MimeType = row["mime_type"].ToString();
            if (row["file_size"] != null && row["file_size"] != DBNull.Value)
                model.FileSize = Convert.ToInt64(row["file_size"]);
            if (row["width"] != null && row["width"] != DBNull.Value)
                model.Width = Convert.ToInt32(row["width"]);
            if (row["height"] != null && row["height"] != DBNull.Value)
                model.Height = Convert.ToInt32(row["height"]);
            if (row["content_hash"] != null && row["content_hash"] != DBNull.Value)
                model.ContentHash = row["content_hash"].ToString();
            if (row["description"] != null && row["description"] != DBNull.Value)
                model.Description = row["description"].ToString();
            if (row["ocr_text"] != null && row["ocr_text"] != DBNull.Value)
                model.OcrText = row["ocr_text"].ToString();
            if (row["description_status"] != null && row["description_status"] != DBNull.Value)
                model.DescriptionStatus = row["description_status"].ToString();
            if (row["is_decorative"] != null && row["is_decorative"] != DBNull.Value)
                model.IsDecorative = Convert.ToBoolean(row["is_decorative"]);
            if (row["metadata"] != null && row["metadata"] != DBNull.Value)
            {
                try
                {
                    model.Metadata = JsonSerializer.Deserialize<Dictionary<string, object>>(row["metadata"].ToString());
                }
                catch { }
            }
            if (row["created_at"] != null && row["created_at"] != DBNull.Value)
                model.CreatedAt = Convert.ToDateTime(row["created_at"]);
            if (row["updated_at"] != null && row["updated_at"] != DBNull.Value)
                model.UpdatedAt = Convert.ToDateTime(row["updated_at"]);

            return model;
        }
    }
}
