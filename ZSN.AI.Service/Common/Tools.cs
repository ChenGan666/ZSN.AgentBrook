using System;
using System.Collections.Generic;
using System.Timers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using LitJson;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using ZSN.Utils.Core.Utils;
using ZSN.AI.Entity;

namespace ZSN.AI.Service.Common
{
    public class Tools
    {

        public static string BaseUrl
        {
            get
            {
                return ConfigHelper.GetString("BaseUrl");
            }
        }

        public static bool CheckLogin(out string msg)
        {
            msg = "";
            bool flag = true;
            object obj = HttpContextHelper.Current.Session.GetString(Keys.SessionKeys.UserID.ToString());
            obj = obj ?? 0;
            return Int32.Parse(obj.ToString())>0;
        }

        public static bool CheckLogin(bool redirect = true)
        {
            string msg;
            if (Tools.CheckLogin(out msg))
                return true;
            if (!redirect)
            {
                HttpContextHelper.Current.Response.WriteAsync("登录验证失败!");
                return false;
            }
            if (!ZSN.Utils.Core.Utils.Tools.IsPhoneAccess())
            {
                HttpContextHelper.Current.Response.WriteAsync("<script>top.lastURL='" + HttpContextHelper.Current.Request.PathAndQuery() + "';top.currentWindow=window;top.login();</script>");
            }
            return false;
        }

        public static bool CheckReferrer(bool isEnd = true)
        {
            var urlReferrer = HttpContextHelper.Current.Request.UrlReferrer();
            if (urlReferrer == null)
            {
                if (isEnd)
                {
                    HttpContextHelper.Current.Response.Clear();
                    HttpContextHelper.Current.Response.WriteAsync("访问地址错误!");
                }
                return false;
            }
            int num = HttpContextHelper.Current.Request.Host.Value.Equals(urlReferrer.GetHost(), StringComparison.CurrentCultureIgnoreCase) ? 1 : 0;
            if (!(num == 0 & isEnd))
                return num != 0;
            HttpContextHelper.Current.Response.Clear();
            HttpContextHelper.Current.Response.WriteAsync("访问地址错误!");
            return num != 0;
        }

        
        public static RouteValueDictionary GetRouteValueDictionary()
        {
            System.Collections.Generic.Dictionary<string, object> dictionary = new System.Collections.Generic.Dictionary<string, object>();
            string query = HttpContextHelper.Current.Request.QueryString.Value??"";
            if (query.IsNullOrEmpty())
                return new RouteValueDictionary((IDictionary<string, object>)dictionary);
            string str1 = query.TrimStart('?');
            char[] chArray1 = new char[1] { '&' };
            foreach (string str2 in str1.Split(chArray1))
            {
                char[] chArray2 = new char[1] { '=' };
                string[] strArray = str2.Split(chArray2);
                if (strArray.Length >= 2)
                    dictionary.Add(strArray[0], (object)strArray[1]);
            }
            return new RouteValueDictionary((IDictionary<string, object>)dictionary);
        }

        public static string SerializeObject(object obj)
        {
            return JsonMapper.ToJson(obj);
        }
    }
}
