using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Runtime.Serialization.Json;
using System.Text;

namespace ZSN.Utils.Core.Helpers
{
    /// <summary>
    ///     邮件发送帮助类
    /// </summary>
    public class MailHelper
    {
        private const int Limit = 10;
        private static readonly string[] MailFrom = { "test@qq.com", "test@aliyun.com" };
        private static readonly string[] Pwd = { "pwd1", "pwd2" };

        private static readonly SmtpClient[] Smtp =
        {
            new SmtpClient
            {
                Credentials = new NetworkCredential(MailFrom[0], Pwd[0]),
                Host = "smtp.qq.com",
                Port = 25,
                EnableSsl = true
            },
            new SmtpClient
            {
                Credentials = new NetworkCredential(MailFrom[1], Pwd[1]),
                Host = "smtp.aliyun.com",
                Port = 25
            }
        };

        private static readonly Dictionary<string, string> NickNameDic = new Dictionary<string, string>
        {
            {"27.0.0.1", "localhost"}
        };

        private static DateTime _lastSendTime = DateTime.Now;
        private static int _mailCount;

        /// <summary>
        ///     限制发送频率
        /// </summary>
        /// <returns></returns>
        public static bool MailFrecrencyLimit()
        {
            _mailCount++;
            if (_lastSendTime.AddMinutes(30) > DateTime.Now && _mailCount > Limit)
            {
                return true;
            }
            if (_lastSendTime.AddMinutes(30) >= DateTime.Now) return false;
            _lastSendTime = DateTime.Now;
            _mailCount = 0;
            return false;
        }

        /// <summary>
        ///     发送邮件
        /// </summary>
        /// <param name="mailto"></param>
        /// <param name="title"></param>
        /// <param name="body"></param>
        /// <param name="defaultIndex"></param>
        /// <param name="isNeedMethodPath"></param>
        public static void SendMail(string mailto, string title, string body, int defaultIndex = 1,
            bool isNeedMethodPath = true)
        {
            if (MailFrecrencyLimit()) return;
            if (defaultIndex >= MailFrom.Length) return;
            try
            {
                // 打印调用方法路径信息
                if (isNeedMethodPath)
                {
                    var fromHandle = "<br><br><br>-------调用路径--------<br>";
                    var st = new StackTrace();
                    var stacks = st.GetFrames();
                    if (stacks != null)
                    {
                        foreach (var stack in stacks)
                        {
                            var stackMethod = stack.GetMethod();
                            fromHandle +=
                                $"Class:{stackMethod.DeclaringType?.FullName ?? ""} , Method:{stackMethod.Name}";
                            fromHandle += "<br>------------------------<br>";
                        }
                    }
                    body += fromHandle;
                    body += "<br><br><br>-----可能的IP地址------<br>";
                    body += GetLocalIp() + "<br>------------------------<br>";
                }
                var fromAddress = new MailAddress(MailFrom[defaultIndex], "监控助手");
                var message = new MailMessage
                {
                    Body = body,
                    From = fromAddress,
                    Subject = title,
                    IsBodyHtml = true
                };

                var listMail = mailto.Split(';').ToList();
                listMail.ForEach(p => { if (!string.IsNullOrEmpty(p)) message.To.Add(new MailAddress(p)); });

                Smtp[defaultIndex].Send(message);
            }
            catch (Exception)
            {
                if (defaultIndex < MailFrom.Length)
                    SendMail(mailto, title, body, defaultIndex + 1, isNeedMethodPath);
            }
        }

        /// <summary>
        ///     获取本地IP
        /// </summary>
        /// <returns></returns>
        public static string GetLocalIp()
        {
            var hostname = Dns.GetHostName();
            var localhost = Dns.GetHostEntry(hostname);
            var lst = localhost.AddressList.Where(p => p.ToString().Split('.').Count() == 4).ToList();
            var ip = "";
            foreach (var ipAddress in lst)
            {
                if (NickNameDic.Keys.Contains(ipAddress.ToString()))
                {
                    ip = NickNameDic[ipAddress.ToString()];
                    break;
                }
            }
            if (ip == "") lst.ForEach(p => ip += $"Ip:{p.ToString()}<br>");
            return ip;
        }

        /// <summary>
        ///     通过api发送邮件，5分钟限制调用一次
        /// </summary>
        /// <param name="mailto"></param>
        /// <param name="title"></param>
        /// <param name="body"></param>
        /// <returns>服务器成功与否信息</returns>
        public static string SendMailWithApi(string mailto, string title, string body)
        {
            try
            {
                var url = ProxyHelper.ApiUrl + "/UtiHelper/SendMail";
                var json = JsonSerializer(new MailStruct { Mailto = mailto, Title = title, Message = body });
                return PostWebContent(url, param: json);
            }
            catch (Exception e)
            {
                return "faild(local):" + e;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="url"></param>
        /// <param name="referer"></param>
        /// <param name="param"></param>
        /// <param name="proxy"></param>
        /// <param name="agent"></param>
        /// <param name="timeout"></param>
        /// <param name="cookieContainer"></param>
        /// <param name="cookies"></param>
        /// <param name="requestOutSide"></param>
        /// <returns></returns>
        private static string PostWebContent(string url, string referer = "", string param = "", string proxy = "",
            string agent = "", int timeout = 20000, CookieContainer cookieContainer = null, string cookies = "",
            HttpWebRequest requestOutSide = null)
        {
            if (string.IsNullOrEmpty(agent))
            {
                agent =
                    "Mozilla/5.0 (iPhone; CPU iPhone OS 8_1_2 like Mac OS X) AppleWebKit/600.1.4 (KHTML, like Gecko) Mobile/12B440 MicroMessenger/6.0.2 NetType/WIFI";
            }

            var request = requestOutSide ?? WebRequest.Create(url) as HttpWebRequest;
            request.UserAgent = agent;
            request.Referer = referer;
            request.Timeout = timeout;
            request.Method = "POST";
            if (!string.IsNullOrEmpty(cookies))
            {
                request.Headers.Set("Cookie", cookies);
            }

            if (!string.IsNullOrEmpty(param))
            {
                var l_data = Encoding.UTF8.GetBytes(param);
                request.ContentLength = l_data.Length;
                using (var newStream = request.GetRequestStream())
                {
                    newStream.Write(l_data, 0, l_data.Length);
                    newStream.Close();
                }
            }
            if (cookieContainer != null)
            {
                request.CookieContainer = cookieContainer;
            }
            else
            {
                request.CookieContainer = new CookieContainer();
            }
            if (!string.IsNullOrEmpty(proxy))
            {
                var arr = proxy.Split(':');
                if (arr.Length > 0)
                {
                    var ip = arr[0];
                    var port = 80;
                    if (arr.Length > 1)
                    {
                        port = Convert.ToInt32(arr[1]);
                    }
                    request.Proxy = new WebProxy(ip, port);
                }
            }


            using (var response = request.GetResponse() as HttpWebResponse)
            {
                if (cookieContainer != null)
                {
                    foreach (Cookie cookie in response.Cookies)
                    {
                        cookieContainer.Add(cookie);
                    }
                }
                using (var steam = response.GetResponseStream())
                {
                    using (var reader = new StreamReader(response.GetResponseStream(), Encoding.UTF8))
                    {
                        return reader.ReadToEnd();
                    }
                }
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="t"></param>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        private static string JsonSerializer<T>(T t)
        {
            var ser = new DataContractJsonSerializer(typeof(T));
            var ms = new MemoryStream();
            ser.WriteObject(ms, t);
            var jsonString = Encoding.UTF8.GetString(ms.ToArray());
            ms.Close();
            return jsonString;
        }

        public struct MailStruct
        {
            public string Mailto;
            public string Message;
            public string Title;
        }
    }
}