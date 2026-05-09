using log4net.Core;
using Microsoft.AspNetCore.Mvc;
using Senparc.NeuChar.App.AppStore;
using System.Security.Cryptography;
using System.Security.Principal;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Utils;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ErrorCode = ZSN.AI.Entity.ErrorCode;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [Area("Manage")]
    public class AuthorizeController : AdminBaseController
    {
        public IActionResult Index()
        {
            return View();
        }
        [AdminAttributes(CheckLogin = false, CheckUrl = false)]
        public IActionResult Login()
        {
            return View();
        }

        [AdminAttributes(CheckLogin = false, CheckUrl = false, CheckPermissions = false)]
        [HttpPost]
        public JsonMsg<UserInfoAccess> doLogin(string username, string password)
        {
            if (username.IsNullOrEmpty() || password.IsNullOrEmpty())
            {
                return JsonMsg<UserInfoAccess>.Error(null, ErrorCode.AccountError);
            }
            UserInfo user = UserInfoBussiness.GetModel(username.SecureSQL());
            if (user == null || string.Compare(user.UPWD, UserInfoBussiness.GetUserEncryptionPassword(user.UserID.ToString(), password.Trim()), false) != 0)
            {
                return JsonMsg<UserInfoAccess>.Error(null, ErrorCode.AccountError);
            }
            if (user.UState == 1)
            {
                return JsonMsg<UserInfoAccess>.Error(null, ErrorCode.AccountLock);
            }

            HttpContext.Session.Clear();
            HttpContext.Session.SetString(Keys.SessionKeys.UserID.ToString(), user.UserID.ToString());
            //HttpContext.Session.SetString(Keys.SessionKeys.BaseUrl.ToString(), base.Url.Content("~/"));
            //HttpContext.Session.SetString(Keys.SessionKeys.UserName.ToString(), user.UName);
            //base.Response.Cookies.Append(Keys.SessionKeys.UserID.ToString(), user.UserID.ToString(), new CookieOptions
            //{
            //    Expires = UserService.CurrentDateTime.AddDays(7.0)
            //});
            UserService.SetLoginRemember(user.UserID);

            UserInfoAccess userInfoAccess = new UserInfoAccess();
            userInfoAccess.UserID = user.UserID;
            userInfoAccess.Token = UserInfoBussiness.GetTokenByUserId(user.UserID);

            return JsonMsg<UserInfoAccess>.OK(userInfoAccess, "登入成功!");
        }

        [AdminAttributes(CheckLogin = false, CheckUrl = false, CheckPermissions = false)]
        public IActionResult Quit()
        {
            HttpContext.Session.Clear();
            base.Response.Cookies.Append(Keys.SessionKeys.UserID.ToString(), "", new CookieOptions
            {
                Expires = DateTime.Now.AddDays(-1.0)
            });
            base.Response.Cookies.Append(Keys.SessionKeys.UserKey.ToString(), "", new CookieOptions
            {
                Expires = DateTime.Now.AddDays(-1.0)
            });
            return Redirect("/Manage/Authorize/Login");
        }
    }
}
