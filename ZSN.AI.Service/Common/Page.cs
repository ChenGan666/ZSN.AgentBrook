using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.Utils;
using Microsoft.AspNetCore.Http;

namespace ZSN.AI.Service.Common
{
    public class Page
    {
        public static string BodyStyle
        {
            get
            {
                var v = HttpContextHelper.Session.GetString(Keys.SessionKeys.PageBodyStyle.ToString());
                HttpContextHelper.Session.SetString(Keys.SessionKeys.PageBodyStyle.ToString(), "");
                return v;
            }
            set => HttpContextHelper.Session.SetString(Keys.SessionKeys.PageBodyStyle.ToString(), value);
        }

        public static bool CheckApp { get; set; }
    }
}
