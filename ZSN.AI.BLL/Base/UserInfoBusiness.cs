using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Utils;
using Lucene.Net.Util;

namespace ZSN.AI.BLL
{
    public partial class UserInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "BaseDb";
        #endregion
		#region tb_user_info
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static int Add(UserInfo model)
		{
			return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(UserInfo model)
		{
			return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_Update(model);
		}

        public static bool UpdatePassword(UserInfo model)
        {
            return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_UpdatePassword(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(Int32 userID)
		{
			return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_Delete(userID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string userIDlist)
		{
			return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_DeleteList(userIDlist);
		}
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.UserInfo GetModel(Int32 userID)
		{
			return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_GetModel(userID);
		}
        public static ZSN.AI.Entity.UserInfo GetModel(string username)
        {
            return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_GetModel(username);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
		public static List<UserInfo> GetList(string strWhere = "")
        {
            return UserInfoDataSet_ToList(DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<UserInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return UserInfoDataSet_ToList(DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<UserInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return UserInfoDataSet_ToList(DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
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
		public static List<UserInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "UserID")
		{
            return UserInfoDataSet_ToList(DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<UserInfo> UserInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<UserInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetUserInfo(ConnectionName).UserInfo_DataRowToModel(r));
            }
            return list;
		}

        public static string EncryptionPassword(string password)
        {
            if (password.IsNullOrEmpty())
                return "";
            HashEncrypt hashEncrypt = new HashEncrypt();
            return hashEncrypt.MD5System(hashEncrypt.MD5System(password));
        }
        public static string GetUserEncryptionPassword(string userID, string password)
        {
            if (!password.IsNullOrEmpty() && !userID.IsNullOrEmpty())
                return EncryptionPassword(userID.Trim().ToLower() + password.Trim());
            return "";
        }
        public static string GetTokenByUserId(int userID)
        {
            Guid guid = Guid.NewGuid();
            return (userID.ToString() + "|" + guid.ToString()).DesEncrypt();
        }
        #endregion

        #region 权限处理
        
        /// <summary>
        /// 用户权限为 Json
        /// </summary>
        /// <returns></returns>
        public static string GetUserPopedomToJsonStr()
        {
            string json = "";
            List<MenuInfo> dt = MenuInfoBussiness.GetList();
            foreach (MenuInfo dr in dt)
            {
                if (dr.ParentID == "00000000-0000-0000-0000-000000000000")
                {
                    json = json + "{data:\"" + dr.Title + "\",attr:{id:\"t_" + dr.ID + "\",value:\"" + dr.ID + "\"},children:[" + GetUserPopedomToJsonStr_loop(dt, dr.ID) + "]},";
                }
            }
            return ZSN.Utils.Core.Utils.Utils.ReSQLSetTxt(json);
        }
        public static string GetUserPopedomToJsonStr_loop(List<MenuInfo> dt, string PopedomID)
        {
            string json = "";

            foreach (MenuInfo dr in dt.FindAll(x => x.ParentID == PopedomID))
            {

                json = json + "{data:\"" + dr.Title + "\",attr:{id:\"t_" + dr.ID + "\",value:\"" + dr.ID + "\"},children:[" + GetUserPopedomToJsonStr_loop(dt, dr.ID) + "]},";

            }
            return ZSN.Utils.Core.Utils.Utils.ReSQLSetTxt(json);
        }

        /// <summary>
        /// 根据用户权限代码，重设菜单数据,匹配权限代码之上的所有菜单项
        /// </summary>
        public static List<MenuInfo> ReSetMenuByUserPopedom(string UserPopedomCode)
        {
            List<MenuInfo> _menu = MenuInfoBussiness.GetList();
            List<MenuInfo> _new_menu = new List<MenuInfo>();
            if (!UserPopedomCode.IsNullOrEmpty())
            {
                string[] _pCode = UserPopedomCode.Split(",");
                if (_pCode.Length > 0) {
                    foreach (string _code in _pCode) {
                        MenuInfo _f_menu = _menu.Find(x => x.ID == _code);
                        if (_f_menu != null)
                        {
                            _new_menu.Add(_f_menu);
                        }
                        _new_menu.AddRange(GetUpMenu(_menu, _code));
                        _new_menu.AddRange(GetDownMenu(_menu, _code));
                    }
                }
            }

            return _new_menu;
        }
        public static List<MenuInfo> GetUpMenu(List<MenuInfo> _menu,string pID)
        {
            List<MenuInfo> _new_menu = new List<MenuInfo>();
            MenuInfo _f_menu = _menu.Find(x => x.ID == pID);
            if (_f_menu != null)
            {
                _new_menu.Add(_f_menu);
                _new_menu.AddRange(GetUpMenu(_menu, _f_menu.ParentID));
            }
            return _new_menu;
        }
        public static List<MenuInfo> GetDownMenu(List<MenuInfo> _menu, string pID)
        {
            List<MenuInfo> _new_menu = new List<MenuInfo>();
            List<MenuInfo> _f_menu = _menu.FindAll(x => x.ParentID == pID);
            if (_f_menu != null)
            {
                _new_menu.AddRange(_f_menu);
                foreach (MenuInfo m in _f_menu)
                {
                    _new_menu.AddRange(GetDownMenu(_menu, m.ID));
                }
            }
            return _new_menu;
        }

        public static string GetDownMenuCodeStr(List<MenuInfo> _menu, string pID) {
            List<MenuInfo> menus = GetDownMenu(_menu, pID);

            return string.Join(",", menus.Select(m => m.ID.ToString()));
        }

        #endregion
    }
}
