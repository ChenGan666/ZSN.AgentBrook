using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using Google.Protobuf.WellKnownTypes;
namespace ZSN.AI.BLL
{
    public partial class CompanyInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "BaseDb";
        #endregion
		#region tb_company_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(CompanyInfo model)
		{
			int CompanyID = DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_Add(model);
            if (CompanyID>0)
            {
                ApisettingsInfo apisettings = new ApisettingsInfo();
                apisettings.AppID = model.AppID;
                apisettings.CompanyID = CompanyID;
                apisettings.SecretKey = model.SecretKey;
                apisettings.SettingName = model.CFullName;
                apisettings.CreateTime = DateTime.Now;
                apisettings.UpdateTime = DateTime.Now;
                ApisettingsInfoBussiness.Add(apisettings);
            }
            return CompanyID;
        }
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(CompanyInfo model)
		{
            if (DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_Update(model))
            {
                ApisettingsInfo apisettings = ApisettingsInfoBussiness.GetModelByAppID(model.AppID);
                if (apisettings != null)
                {
                    apisettings.AppID = model.AppID;
                    apisettings.SecretKey = model.SecretKey;
                    apisettings.UpdateTime = DateTime.Now;

                    return ApisettingsInfoBussiness.Update(apisettings);
                }
                else
                {
                    apisettings = new ApisettingsInfo();
                    apisettings.AppID = model.AppID;
                    apisettings.CompanyID = model.CompanyID;
                    apisettings.SecretKey = model.SecretKey;
                    apisettings.SettingName = model.CFullName;
                    apisettings.CreateTime = DateTime.Now;
                    apisettings.UpdateTime = DateTime.Now;
                    return ApisettingsInfoBussiness.Add(apisettings) > 0;
                }
            }
            else
            {
                return false;
            }
               
        }
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 companyID)
		{
			return DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_Delete(companyID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string companyIDlist)
		{
			return DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_DeleteList(companyIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.CompanyInfo GetModel()
		{
            ZSN.AI.Entity.CompanyInfo companyInfo = DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_GetModel();
            if (companyInfo != null)
            {
                ApisettingsInfo apisettings = ApisettingsInfoBussiness.GetModelByCompanyID(companyInfo.CompanyID);
                if (apisettings != null)
                {
                    companyInfo.AppID = apisettings.AppID;
                    companyInfo.SecretKey = apisettings.SecretKey;
                }
            }
            return companyInfo;

        }
        public static ZSN.AI.Entity.CompanyInfo GetModel(Int32 companyID)
        {
            ZSN.AI.Entity.CompanyInfo companyInfo = DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_GetModel(companyID); 
            if (companyInfo != null)
            {
                ApisettingsInfo apisettings = ApisettingsInfoBussiness.GetModelByCompanyID(companyInfo.CompanyID);
                if (apisettings != null)
                {
                    companyInfo.AppID = apisettings.AppID;
                    companyInfo.SecretKey = apisettings.SecretKey;
                }
            }
            return companyInfo;
        }
        public static ZSN.AI.Entity.CompanyInfo GetModelByAppID(string AppID) { 
            ApisettingsInfo apisettings = ApisettingsInfoBussiness.GetModelByAppID(AppID);
            if(apisettings != null)
            {
                ZSN.AI.Entity.CompanyInfo companyInfo = GetModel(apisettings.CompanyID);
                if (companyInfo != null)
                {
                    companyInfo.AppID = apisettings.AppID;
                    companyInfo.SecretKey = apisettings.SecretKey;
                    return companyInfo;
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<CompanyInfo> GetList(string strWhere = "")
        {
            return CompanyInfoDataSet_ToList(DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<CompanyInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return CompanyInfoDataSet_ToList(DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<CompanyInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return CompanyInfoDataSet_ToList(DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
		/// <summary>
        /// 分页获取数据列表
        /// </summary>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页标</param>
        /// <param name="strWhere">查询条件</param>
        /// <param name="pagetotal">总页数</param>
        /// <param name="total">总数</param>
        /// <param name="orderType">排序规则， 默认降序，1降序，0升序</param>
        /// <param name="showName">显示字段，默认全部</param>
        /// <param name="orderKey">排序key，默认主键</param>
        /// <returns></returns>
		public static List<CompanyInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "CompanyID")
		{
            return CompanyInfoDataSet_ToList(DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<CompanyInfo> CompanyInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<CompanyInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetCompanyInfo(ConnectionName).CompanyInfo_DataRowToModel(r));
            }
            return list;
		}
		#endregion 
	}
}
