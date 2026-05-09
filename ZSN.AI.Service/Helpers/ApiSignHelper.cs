using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Helpers;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;


namespace ZSN.AI.Service.Helpers
{
    public class ApiSignHelper
    {
        public static string GetSign(Dictionary<string, object> dic, string api_secret)
        {
            //var dicKeyList = dic.OrderBy(k => k.Key).Select(k => k.Key).ToList();
            var dicKeyList = dic.OrderBy(k => k.Key.ToLower()).Select(k => k.Key).ToList();
            var tempStr = "";
            var sb = new StringBuilder();

            foreach (string key in dicKeyList)
            {
                string trimmedKey = key.Trim();
                sb.Append(trimmedKey);

                if (dic.TryGetValue(key, out var val) && val != null)
                {
                    if (val is bool b)
                        sb.Append(b.ToString().ToLower());
                    else
                        sb.Append(val.ToString());
                }
            }

            tempStr = sb.ToString().Trim();
            var resultStr2 = tempStr + "AppKEY" + api_secret;

            return EncryptHelper.MD5Encrypt(resultStr2).ToUpper();

        }
        public static string GetSignStr(Dictionary<string, object> dic)
        {
            var dicKeyList = dic.OrderBy(k => k.Key).Select(k => k.Key).ToList();
            var tempStr = "";
            foreach (string key in dicKeyList)
            {
                tempStr += key.Trim() + (dic.ContainsKey(key) ? (dic[key] != null ? dic[key].ToString() : "") : "").Trim();
            }
            return tempStr;

        }
    }
}
