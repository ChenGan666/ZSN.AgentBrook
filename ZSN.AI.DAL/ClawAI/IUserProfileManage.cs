using System;
using System.Collections.Generic;
using System.Data;
using ZSN.AI.Entity;

namespace ZSN.AI.DAL
{
    public partial interface IUserProfileManage
    {
        string SetConnectionName(string connName);

        #region tb_user_profile
        /// <summary>
        /// 增加一条数据
        /// </summary>
        string UserProfile_Add(UserProfileInfo model);

        /// <summary>
        /// 更新一条数据
        /// </summary>
        bool UserProfile_Update(UserProfileInfo model);

        /// <summary>
        /// 删除一条数据
        /// </summary>
        bool UserProfile_Delete(string ProfileID);

        /// <summary>
        /// 批量删除数据
        /// </summary>
        bool UserProfile_DeleteList(string ProfileIDlist);

        /// <summary>
        /// DataRow转Model
        /// </summary>
        UserProfileInfo UserProfile_DataRowToModel(DataRow row);

        /// <summary>
        /// 得到一个对象实体
        /// </summary>
        UserProfileInfo UserProfile_GetModel(string ProfileID);

        /// <summary>
        /// 根据用户ID和应用ID获取画像
        /// </summary>
        UserProfileInfo UserProfile_GetByMemberAndApp(string MemberID, string AppID);

        /// <summary>
        /// 获得数据列表
        /// </summary>
        DataSet UserProfile_GetList(string strWhere);

        /// <summary>
        /// 获得前几行数据
        /// </summary>
        DataSet UserProfile_GetList(int top, string strWhere, string filedOrder);

        /// <summary>
        /// 获取记录总数
        /// </summary>
        int UserProfile_GetRecordCount(string strWhere);

        /// <summary>
        /// 增加交互次数
        /// </summary>
        bool UserProfile_IncrementInteractions(string ProfileID);
        #endregion
    }
}
