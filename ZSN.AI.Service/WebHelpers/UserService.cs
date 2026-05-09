using System;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Routing;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.BLL;
using ZSN.AI.Service.WebHelpers;

namespace ZSN.AI.Service.WebHelpers
{
    public class UserService : Controller
    {
        public static int CurrentUserID
        {
            get
            {
                try
                {
                    object obj = HttpContextHelper.Session?.GetString(Keys.SessionKeys.UserID.ToString());
                    return obj == null ? 0 : Int32.Parse(obj.ToString());
                }
                catch
                {
                    return 0;
                }
            }
        }

        public static UserInfo CurrentUser
        {
            get
            {
                int currentUserId = UserService.CurrentUserID;
                if (currentUserId == 0)
                    return null;
                return UserInfoBussiness.GetModel(currentUserId);
            }
        }


        public static DateTime CurrentDateTime
        {
            get
            {
                return DateTime.Now;
            }
        }

        /// <summary>
        /// 登陆记忆
        /// </summary>
        /// <param name="id"></param>
        public static void SetLoginRemember(int id)
        {
            var d = ConfigHelper.GetInt("DictionarySessionDay",1);
            var key = EncryptHelper.AESEncrypt($"{id}||{DateTime.Now}");
            HttpContextHelper.Response.Cookies.Append(Keys.SessionKeys.UserKey.ToString(), key, new CookieOptions
            {
                Expires = UserService.CurrentDateTime.AddDays(d)
            });
        }

        /// <summary>
        /// 恢复登录
        /// </summary>
        public static void RememberLogin()
        {
            object obj = HttpContextHelper.Session?.GetString(Keys.SessionKeys.UserID.ToString());
            if (obj != null)
                return;
            var uk = HttpContextHelper.Request?.Cookies["UserKey"];
            if (uk == null)
                return;
            try
            {
                var keys = EncryptHelper.AESDecrypt(uk).Split("||");
                var id = keys[0].ToInt32();
                var day = keys[1].ToDateTime();
                var d = ConfigHelper.GetInt("DictionarySessionDay", 1);
                if (DateTime.Now >= day.AddDays(d))
                    return;
                var user =UserInfoBussiness.GetModel(id);
                if(user == null) return;

                HttpContextHelper.Session.SetString(Keys.SessionKeys.UserID.ToString(), id.ToString());
                HttpContextHelper.Session.SetString(Keys.SessionKeys.UserUniqueID.ToString(), id.ToString());
                HttpContextHelper.Session.SetString(Keys.SessionKeys.BaseUrl.ToString(),"/");
                HttpContextHelper.Session.SetString(Keys.SessionKeys.UserName.ToString(), user.UName);
                /*
                HttpContextHelper.Response.Cookies.Append(Keys.SessionKeys.UserID.ToString(), user.UserID.ToString(), new CookieOptions
                {
                    Expires = UserService.CurrentDateTime.AddDays(7.0)
                });
                */
            }
            catch (Exception e)
            {
                DefaultLogService.AddOperationLog(1, e, "记忆登录");
            }
        }
    }
}
