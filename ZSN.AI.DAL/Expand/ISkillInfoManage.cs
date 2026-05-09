using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface ISkillInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_skill_info
        string SkillInfo_Add(SkillInfo model);
        bool SkillInfo_Update(SkillInfo model);
        bool SkillInfo_Delete(string skillID); 
        bool SkillInfo_DeleteList(string skillIDlist);
        SkillInfo SkillInfo_DataRowToModel(DataRow row);
        SkillInfo SkillInfo_GetModel(string skillID); 
        DataSet SkillInfo_GetList(string strWhere);
        DataSet SkillInfo_GetList(int top, string strWhere, string filedOrder);
        int SkillInfo_GetRecordCount(string strWhere);
        DataSet SkillInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable SkillInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
