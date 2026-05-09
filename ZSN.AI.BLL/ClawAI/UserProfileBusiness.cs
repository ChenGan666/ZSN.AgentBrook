using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;

namespace ZSN.AI.BLL
{
    public partial class UserProfileBusiness
    {
        #region 基础信息
        private const string ConnectionName = "KnowledgeBaseDb";
        #endregion

        #region tb_user_profile
        /// <summary>
        /// 增加一条数据
        /// </summary>
        public static string Add(UserProfileInfo model)
        {
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_Add(model);
        }

        /// <summary>
        /// 更新一条数据
        /// </summary>
        public static bool Update(UserProfileInfo model)
        {
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_Update(model);
        }

        /// <summary>
        /// 删除一条数据
        /// </summary>
        public static bool Delete(string ProfileID)
        {
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_Delete(ProfileID);
        }

        /// <summary>
        /// 批量删除数据
        /// </summary>
        public static bool DeleteList(string ProfileIDlist)
        {
            ProfileIDlist = ZSN.Utils.Core.Utils.StringUtil.QuoteSeparatedItems(ProfileIDlist, ',', '\'');
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_DeleteList(ProfileIDlist);
        }

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        public static UserProfileInfo GetModel(string ProfileID)
        {
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_GetModel(ProfileID);
        }

        /// <summary>
        /// 根据用户ID和应用ID获取画像
        /// </summary>
        public static UserProfileInfo GetByMemberAndApp(string MemberID, string AppID)
        {
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_GetByMemberAndApp(MemberID, AppID);
        }

        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<UserProfileInfo> GetList(string strWhere = "")
        {
            return UserProfileDataSet_ToList(DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_GetList(strWhere).Tables[0]);
        }

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        public static List<UserProfileInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return UserProfileDataSet_ToList(DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_GetList(top, strWhere, filedOrder).Tables[0]);
        }

        /// <summary>
        /// 获取记录总数
        /// </summary>
        public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_GetRecordCount(strWhere);
        }

        /// <summary>
        /// 增加交互次数
        /// </summary>
        public static bool IncrementInteractions(string ProfileID)
        {
            return DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_IncrementInteractions(ProfileID);
        }

        private static List<UserProfileInfo> UserProfileDataSet_ToList(DataTable dt)
        {
            var rows = dt.Rows;
            var list = new List<UserProfileInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetUserProfile(ConnectionName).UserProfile_DataRowToModel(r));
            }
            return list;
        }
        #endregion
    }
}
