using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Linq;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
namespace ZSN.AI.BLL
{
    public partial class KnowledgeBaseFileChunkInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion
		#region tb_knowledge_base_file_chunk_info
		
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string chunkID, string KnowledgeBaseID)
		{
            string[] chunkIDArr = (chunkID ?? string.Empty).Split(new char[] { ',' }, StringSplitOptions.RemoveEmptyEntries);
            foreach (var raw in chunkIDArr)
            {
                var id = raw.Trim().Trim('\'');
                if (id.IsNullOrEmpty() == false)
                {
                    if (!DatabaseProvider.GetKnowledgeBaseFileChunkInfo(ConnectionName).KnowledgeBaseFileChunkInfo_Delete(id, KnowledgeBaseID))
                    {
                        break;
                    }
                }
            }
            return true;

        }

        /// <summary>
        /// 分页获取数据列表
        /// </summary>
        public static List<KnowledgeBaseFileChunkInfo> GetListByPage(string KnowledgeBaseID, int size, int index,string where, out int pagetotal, out int total)
        {
            return KnowledgeBaseFileInfoDataSet_ToList(DatabaseProvider.GetKnowledgeBaseFileChunkInfo(ConnectionName).KnowledgeBaseFileChunkInfo_GetListByPage(KnowledgeBaseID, size, index, where, out  pagetotal, out  total));
        }
        private static List<KnowledgeBaseFileChunkInfo> KnowledgeBaseFileInfoDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<KnowledgeBaseFileChunkInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetKnowledgeBaseFileChunkInfo(ConnectionName).KnowledgeBaseFileChunkInfo_DataRowToModel(r));
            }
            return list;
        }
        #endregion
    }
}
