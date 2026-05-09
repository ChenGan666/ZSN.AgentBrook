using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;
using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.Extensions;
using ZSN.AI.Service.WebHelpers;
using ZSN.AI.Service.Common;
using ZSN.AI.Entity;
using ZSN.AI.BLL;

namespace ZSN.AgentBrook.Web.Manage.Attributes
{
    public class AdminAttributes: ActionFilterAttribute
    {
        public bool CheckLogin { get; set; } = true;
        public bool CheckUrl { get; set; } = true;
        public bool CheckPermissions { get; set; } = true;

        public override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            base.OnActionExecuting(filterContext);

            string msg;
            string msg2;
            if (CheckUrl && !Tools.CheckReferrer(false))
            {
                filterContext.Result = new ContentResult
                {
                    Content = "地址验证错误"
                };
            }
            else
            {

                if (CheckLogin)
                {
                    if (LoginCheck(out msg))
                    {
                        if (CheckPermissions)
                        {
                            UserInfo user = UserService.CurrentUser;
                            if (user != null)
                            {
                                
                                if (!PermissionsCheck(filterContext.HttpContext.Request.Path,user.PermissionCode))
                                {
                                    filterContext.Result = new ContentResult
                                    {
                                        Content = "权限不足！"
                                    };
                                }
                                
                            }
                            else
                            {
                                filterContext.Result = new ContentResult
                                {
                                    Content = "<script>top.location='/Manage/Authorize/Login';</script>",
                                    ContentType = "text/html",
                                };
                            }
                        }
                    }
                    else
                    {
                        if (filterContext.HttpContext.Request.IsAjaxRequest())
                        {
                            filterContext.Result = new ContentResult
                            {
                                Content = "{\"loginstatus\":-1, \"url\":\"\"}"
                            };
                        }
                        else
                        {
                            string str = Extensions.UrlEncode(HttpContextHelper.Current.Request.PathAndQuery());
                            if (string.Compare(filterContext.Controller.ToString(), "ZSN.XinLing.Web.Manage.Areas.Manage.Controllers.AuthorizeController.Login", true) != 0)
                            {
                                filterContext.Result = new ContentResult
                                {
                                    Content = "<script>top.location='/Manage/Authorize/Login';</script>",
                                    //Content = "<script>" + (msg.IsNullOrEmpty() ? "" : string.Format("alert('{0}');", msg)) +
                                    //((string.Compare(filterContext.Controller.ToString(), "ZSN.XinLing.Web.Manage.Areas.Manage.Controllers.AuthorizeController.Login", true) == 0) ? ("top.location='/Manage/Authorize/Login'") : ("")) + "</script>",
                                    ContentType = "text/html",
                                };
                            }
                            else
                            {

                            }
                        }
                    }
                }

            }
        }
        protected virtual bool LoginCheck(out string msg)
        {
            UserService.RememberLogin();

            return Tools.CheckLogin(out msg);
        }

       /// <summary>
       /// 权限判断
       /// </summary>
       /// <param name="_CurrentRequestPath"></param>
       /// <param name="PermissionCode"></param>
       /// <returns></returns>
        protected virtual bool PermissionsCheck(string _CurrentRequestPath,string PermissionCode) {
            bool _re = false;

            string[] _pCode = PermissionCode.Split(',');
            List<MenuInfo> list = MenuInfoBussiness.GetList();

            if(list.Exists(x=>x.Url.ToLower() == _CurrentRequestPath.ToLower()))
            {
                foreach (MenuInfo menu in list)
                {
                    if (menu.Url.Contains(_CurrentRequestPath))
                    {
                        //匹配权限代码是否已经存在
                        if (PermissionCode.ToLower().IndexOf(menu.ID.ToLower()) > -1)
                        {
                            _re = true;
                        }
                        else
                        {
                            //匹配是否为该用户拥有权限代码之下的子权限
                            foreach (string _code in _pCode)
                            {
                                string _str = UserInfoBussiness.GetDownMenuCodeStr(list, _code);
                                if (_str.ToLower().IndexOf(menu.ID.ToLower()) > -1)
                                {
                                    _re = true;
                                    break;
                                }
                            }
                        }
                        break;
                    }
                }
            }
            else
            {
                //不在权限列表内的不做判断
                _re = true;
            }

            return _re;
        }
    }
}
