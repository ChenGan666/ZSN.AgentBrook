using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Utils;

namespace ZSN.AI.BLL
{
    public partial class FilesInfoBussiness
    {
	    #region ������Ϣ
        private const string ConnectionName = "ObjectDb";
        #endregion
		#region tbFilesInfo
		/// <summary>
        /// ����һ������
        /// </summary>
		public static int Add(FilesInfo model)
		{
			return DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_Add(model);
		}
		/// <summary>
        /// ����һ������
        /// </summary>
		public static bool Update(FilesInfo model)
		{
			return DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_Update(model);
		}
        /// <summary>
        /// ɾ��һ������
        /// </summary>
		public static bool Delete(string FilesCode)
		{
			return DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_Delete(FilesCode);
		}
        /// <summary>
        /// ����ɾ������
        /// </summary>
		public static bool DeleteList(string FilesCodelist)
		{
            if (FilesCodelist.Trim() != "")
            {
                FilesCodelist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(FilesCodelist, ',', '\'');

                return DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_DeleteList(FilesCodelist);
            }
            else
            {
                return false;
            }
		}
        /// <summary>
        /// �õ�һ������ʵ��
        /// </summary>
		public static ZSN.AI.Entity.FilesInfo GetModel(string FilesCode)
		{
			return DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_GetModel(FilesCode);
		}
        /// <summary>
        /// ��������б�
        /// </summary>
		public static List<FilesInfo> GetList(string strWhere = "")
        {
            return FilesInfoDataSet_ToList(DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// ���ǰ��������
        /// </summary>
		public static List<FilesInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return FilesInfoDataSet_ToList(DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// ��ȡ��¼����
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// ��ҳ��ȡ�����б�
        /// </summary>
		public static List<FilesInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return FilesInfoDataSet_ToList(DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
		/// <summary>
        /// ��ҳ��ȡ�����б�
        /// </summary>
        /// <param name="pageSize">ÿҳ��С</param>
        /// <param name="pageIndex">ҳ��</param>
        /// <param name="strWhere">��ѯ����</param>
        /// <param name="pagetotal">��ҳ��</param>
        /// <param name="total">����</param>
        /// <param name="orderType">������� Ĭ�Ͻ���1����0����</param>
        /// <param name="showName">��ʾ�ֶΣ�Ĭ��ȫ��</param>
        /// <param name="orderKey">����key��Ĭ������</param>
        /// <returns></returns>
		public static List<FilesInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "FilesCode")
		{
            return FilesInfoDataSet_ToList(DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<FilesInfo> FilesInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<FilesInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetFilesInfo(ConnectionName).FilesInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
