using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
namespace ZSN.AI.DAL
{
    public partial interface IWordTemplateInfoManage
    {
        string SetConnectionName(string connName);
        #region tb_word_template_Info
        string WordTemplateInfo_Add(WordTemplateInfo model);
        bool WordTemplateInfo_Update(WordTemplateInfo model);
        bool WordTemplateInfo_Delete(string wordTemplateID); 
        bool WordTemplateInfo_DeleteList(string wordTemplateIDlist);
        WordTemplateInfo WordTemplateInfo_DataRowToModel(DataRow row);
        WordTemplateInfo WordTemplateInfo_GetModel(string wordTemplateID); 
        DataSet WordTemplateInfo_GetList(string strWhere);
        DataSet WordTemplateInfo_GetList(int top, string strWhere, string filedOrder);
        int WordTemplateInfo_GetRecordCount(string strWhere);
        DataSet WordTemplateInfo_GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex);
        DataTable WordTemplateInfo_GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType, string showName, string orderKey);
        #endregion
    }
}
