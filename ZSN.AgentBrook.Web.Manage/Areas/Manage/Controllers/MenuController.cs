using LitJson;
using log4net.Core;
using Microsoft.AspNetCore.Mvc;
using NPOI.SS.UserModel;
using Senparc.NeuChar.App.AppStore;
using System.Data;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Extensions;

using ZSN.Utils.Core.Utils;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using ZSN.AgentBrook.Web.Manage.Attributes;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [Area("Manage")]
    public class MenuController: AdminBaseController
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Empty()
        {
            return View();
        }

        public IActionResult Tree()
        {
            return View();
        }

        public string Tree1()
        {
            List<MenuInfo> allDataTable = MenuInfoBussiness.GetList();
            if (allDataTable.Count == 0)
            {
                return "[]";
            }
            List<MenuInfo> array = allDataTable.FindAll(x=>x.ParentID == Guid.Empty.ToString());
            if (array.Count == 0)
            {
                return "[]";
            }
            List<MenuInfo> array2 = allDataTable.FindAll(x=>x.ParentID == array[0].ID);
            StringBuilder stringBuilder = new StringBuilder("[", 1000);
            MenuInfo dataRow = array[0];
            string text = dataRow.Ico.ToString();

            stringBuilder.Append("{");
            stringBuilder.AppendFormat("\"id\":\"{0}\",", dataRow.ID);
            stringBuilder.AppendFormat("\"title\":\"{0}\",", dataRow.Title);
            stringBuilder.AppendFormat("\"ico\":\"{0}\",", text);
            stringBuilder.AppendFormat("\"link\":\"{0}\",", dataRow.Url);
            stringBuilder.AppendFormat("\"type\":\"{0}\",", "0");
            stringBuilder.AppendFormat("\"model\":\"{0}\",", "0");
            stringBuilder.AppendFormat("\"width\":\"{0}\",", "");
            stringBuilder.AppendFormat("\"height\":\"{0}\",", "");
            stringBuilder.AppendFormat("\"hasChilds\":\"{0}\",", (array2.Count != 0) ? "1" : "0");
            stringBuilder.AppendFormat("\"childs\":[");
            for (int i = 0; i < array2.Count; i++)
            {
                MenuInfo dataRow2 = array2[i];
                string text2 = dataRow2.Ico.ToString();

                List<MenuInfo> array3 = allDataTable.FindAll(x=>x.ParentID == dataRow2.ID.ToString());
                stringBuilder.Append("{");
                stringBuilder.AppendFormat("\"id\":\"{0}\",", dataRow2.ID);
                stringBuilder.AppendFormat("\"title\":\"{0}\",", dataRow2.Title);
                stringBuilder.AppendFormat("\"ico\":\"{0}\",", text2);
                stringBuilder.AppendFormat("\"link\":\"{0}\",", dataRow2.Url);
                stringBuilder.AppendFormat("\"type\":\"{0}\",", "0");
                stringBuilder.AppendFormat("\"model\":\"{0}\",", "0");
                stringBuilder.AppendFormat("\"width\":\"{0}\",", "");
                stringBuilder.AppendFormat("\"height\":\"{0}\",", "");
                stringBuilder.AppendFormat("\"hasChilds\":\"{0}\",", (array3.Count != 0) ? "1" : "0");
                stringBuilder.AppendFormat("\"childs\":[");
                stringBuilder.Append("]");
                stringBuilder.Append("}");
                if (i < array2.Count - 1)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append("]");
            stringBuilder.Append("}");
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }
        public string TreeRefresh()
        {
            string text = base.Request.GetParam("refreshid").SecureSQL();
            if (!text.IsGuid())
            {
                return "[]";
            }

            List<MenuInfo> array = MenuInfoBussiness.GetList("ParentID='" + text + "'");
            StringBuilder stringBuilder = new StringBuilder("[", array.Count * 50);
            int num = array.Count;
            int num2 = 0;
            List<MenuInfo> array2 = array;
            foreach (MenuInfo dataRow in array2)
            {
                string text2 = dataRow.Ico.ToString();

                stringBuilder.Append("{");
                stringBuilder.AppendFormat("\"id\":\"{0}\",", dataRow.ID);
                stringBuilder.AppendFormat("\"title\":\"{0}\",", dataRow.Title);
                stringBuilder.AppendFormat("\"ico\":\"{0}\",", text2);
                stringBuilder.AppendFormat("\"link\":\"{0}\",", "");
                stringBuilder.AppendFormat("\"type\":\"{0}\",", "0");
                stringBuilder.AppendFormat("\"model\":\"{0}\",", "");
                stringBuilder.AppendFormat("\"width\":\"{0}\",", "");
                stringBuilder.AppendFormat("\"height\":\"{0}\",", "");
                stringBuilder.AppendFormat("\"hasChilds\":\"{0}\",", array.FindAll(x=>x.ParentID == dataRow.ID).Count>0 ? "1" : "0");
                stringBuilder.AppendFormat("\"childs\":[");
                stringBuilder.Append("]");
                stringBuilder.Append("}");
                if (num2++ < num - 1)
                {
                    stringBuilder.Append(",");
                }
            }
            stringBuilder.Append("]");
            return stringBuilder.ToString();
        }

        public IActionResult Body()
        {
            return Body(null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Body(IFormCollection collection)
        {
            string str = base.Request.Query["id"];

            MenuInfo menu2 = new MenuInfo();
            Guid test;
            if (str.IsGuid(out test))
            {
                menu2 = MenuInfoBussiness.GetModel(test.ToString());
            }
            if (collection != null && !base.Request.Form["Save"].IsNullOrEmpty())
            {
                string Name = base.Request.Form["Name"];
                string Type = base.Request.Form["Type"];
                string Url = base.Request.Form["Url"];
                string Params = base.Request.Form["Params"];
                string Ico = base.Request.Form["Ico"];
                string IcoColor = base.Request.Form["IcoColor"];
                string MState = base.Request.Form["MState"];

                string oldXML = menu2.Serialize();
                menu2.ID = str;
                menu2.ParentID = base.Request.Form["ParentID"];
                menu2.Title = Name;
                menu2.Sort = MenuInfoBussiness.GetMaxSort(menu2.ParentID);
                menu2.Url = Url;
                menu2.Params = (Params.IsNullOrEmpty() ? null : Params.Trim());
                menu2.Ico = Ico;
                menu2.IcoColor = IcoColor;

                if (!MState.IsNullOrEmpty())
                {
                    menu2.MState = int.Parse(MState);
                }
                else
                {
                    menu2.MState = 0;
                }
                MenuInfoBussiness.Update(menu2);


                string str2 = (menu2.ParentID == string.Empty) ? menu2.ID.ToString() : menu2.ParentID.ToString();
                base.ViewBag.Script = "parent.frames[0].reLoad('" + str2 + "');alert('保存成功!');";

            }
            if (collection != null && !base.Request.Form["Delete"].IsNullOrEmpty())
            {
               string text = (menu2.ParentID == string.Empty) ? menu2.ID.ToString() : menu2.ParentID.ToString();
                base.ViewBag.Script = "parent.frames[0].reLoad('" + text + "');window.location='Body?id=" + text + "&appid=" + base.Request.Query["appid"] + "&tabid=" + base.Request.Query["tabid"] + "';";
            }

            return View(menu2);
        }
        public IActionResult Sort()
        {
            return Sort(null);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Sort(IFormCollection collection)
        {
            string str = base.Request.Query["id"];
           MenuInfo menu2 = MenuInfoBussiness.GetModel(str.SecureSQL());
            List<MenuInfo> list = MenuInfoBussiness.GetList("ParentID='" + menu2.ParentID + "'");
            if (collection != null)
            {
                string text = base.Request.Form["sortapp"];
                if (text.IsNullOrEmpty())
                {
                    return View(list);
                }
                string[] array = text.Split(',');
                for (int i = 0; i < array.Length; i++)
                {
                    Guid test;
                    if (array[i].IsGuid(out test))
                    {
                        MenuInfoBussiness.UpdateSort(test.ToString(), i + 1);
                    }
                }
                string str2 = menu2.ParentID.ToString();
                base.ViewBag.Script = "parent.frames[0].reLoad('" + str2 + "');";
                list = MenuInfoBussiness.GetList("ParentID='" + menu2.ParentID + "'");
            }
            return View(list);
        }


        public IActionResult AddApp()
        {
            return AddApp(null);
        }
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult AddApp(IFormCollection collection)
        {
            string text = base.Request.Query["id"];
            if (collection != null)
            {
                MenuInfo menu = MenuInfoBussiness.GetModel(text.SecureSQL());

                if (collection != null && !base.Request.Form["Save"].IsNullOrEmpty())
                {
                    string Name = base.Request.Form["Name"];
                    string Type = base.Request.Form["Type"];
                    string Url = base.Request.Form["Url"];
                    string Params = base.Request.Form["Params"];
                    string Ico = base.Request.Form["Ico"];
                    string IcoColor = base.Request.Form["IcoColor"];
                    string MState = base.Request.Form["MState"];
                    MenuInfo menu2 = new MenuInfo();
                    menu2.ID = Guid.NewGuid().ToString();
                    menu2.ParentID = text;
                    menu2.Title = Name;
                    menu2.Sort = MenuInfoBussiness.GetMaxSort(menu2.ParentID);
                    menu2.Url = Url;
                    menu2.Params = (Params.IsNullOrEmpty() ? null : Params.Trim());
                    menu2.Ico = Ico;
                    menu2.IcoColor = IcoColor;
                    
                    if (!MState.IsNullOrEmpty())
                    {
                        menu2.MState = int.Parse(MState);
                    }
                    else
                    {
                        menu2.MState = 0;
                    }
                    MenuInfoBussiness.Add(menu2);

                    string str2 = text;
                    base.ViewBag.Script = "alert('添加成功');parent.frames[0].reLoad('" + str2 + "');";

                }
            }

            return View();
        }
        public IActionResult Menu()
        {
            return View();
        }

        public IActionResult Buttons()
        {
            string text = "&appid=" + base.Request.Query["appid"] + "&tabid=" + base.Request.Query["tabid"];
            base.ViewBag.Query = text;
            return View();
        }


        public IActionResult Refresh()
        {
            return View();
        }

        public string Refresh1()
        {
            return "更新完成!";
        }
    }
}
