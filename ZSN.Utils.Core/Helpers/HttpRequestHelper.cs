using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Web;
using ZSN.Utils.Core.Extensions;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace ZSN.Utils.Core.Helpers
{
    public static class HttpRequestHelper
    {
        /// <summary>
        /// Sets the cert policy.
        /// </summary>
        public static void SetCertificatePolicy()
        {
            ServicePointManager.ServerCertificateValidationCallback += RemoteCertificateValidate;
        }

        /// <summary>
        /// Remotes the certificate validate.
        /// </summary>
        private static bool RemoteCertificateValidate(
           object sender, X509Certificate cert,
            X509Chain chain, SslPolicyErrors error)
        {
            // trust any certificate!!!
            System.Console.WriteLine("Warning, trust any certificate");
            return true;
        }

        #region 同步方法

        #region 同步Get
        /// <summary>
        /// 使用Get方法获取字符串结果（加入Cookie）
        /// </summary>
        /// <param name="url"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HttpGet(out HttpStatusCode StatusCode,string url, Dictionary<string, string> headers = null, Encoding encoding = null, int timeOut = 60000)
        {
            SetCertificatePolicy();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeOut;

            //if (cookieContainer != null)
            //{
            //    request.CookieContainer = cookieContainer;
            //}

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            //if (cookieContainer != null)
            //{
            //    response.Cookies = cookieContainer.GetCookies(response.ResponseUri);
            //}
            StatusCode = response.StatusCode;
            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = myStreamReader.ReadToEnd();
                    return retString;
                }
            }
        }

        /// <summary>
        /// 使用Get方式下载文件
        /// </summary>
        /// <param name="url"></param>
        /// <param name="dirPath"></param>
        /// <param name="fileName"></param>
        /// <param name="isUpdate">是否删除旧文件</param>
        /// <returns></returns>
        public static string HttpGetFile(string url, string dirPath, string fileName = "", bool isUpdate = true)
        {
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            SetCertificatePolicy();
            var request = WebRequest.Create(url) as HttpWebRequest;
            //发送请求并获取相应回应数据
            var response = request.GetResponse() as HttpWebResponse;
            //直到request.GetResponse()程序才开始向目标网页发送Post请求
            var responseStream = response.GetResponseStream();
            var mt = "filename=\"(?<name>.+?)\"";
            var h = response.Headers["Content-Disposition"];
            var mat = Regex.Match(h, mt);
            var oldName = mat.Groups["name"].ToString();
            var newName = fileName.IsNullOrEmpty() ? oldName : fileName + Path.GetExtension(oldName);
            var filePath = PathHelper.Combine(dirPath, newName);
            if (File.Exists(filePath))
            {
                if (isUpdate)
                    File.Delete(filePath);
                else
                    return filePath;
            }

            //创建本地文件写入流
            Stream stream = new FileStream(filePath, FileMode.Create);
            var bArr = new byte[1024];
            var size = responseStream.Read(bArr, 0, (int)bArr.Length);
            while (size > 0)
            {
                stream.Write(bArr, 0, size);
                size = responseStream.Read(bArr, 0, (int)bArr.Length);
            }
            stream.Close();
            responseStream.Close();
            return filePath;
        }

        public static string HttpGetFile(string url, string dirPath, string fileName, out int fileSize)
        {
            fileSize = 0;
            if (!Directory.Exists(dirPath))
            {
                Directory.CreateDirectory(dirPath);
            }
            ConsoleLogHelper.WriteLine("服务器地址：" + dirPath);
            ConsoleLogHelper.WriteLine("默认文件名：" + fileName);
            SetCertificatePolicy();
            var request = WebRequest.Create(url) as HttpWebRequest;
            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.87 Safari/537.36 QQBrowser/9.2.5748.400";
            //发送请求并获取相应回应数据
            var response = request.GetResponse() as HttpWebResponse;
            //直到request.GetResponse()程序才开始向目标网页发送Post请求
            var responseStream = response.GetResponseStream();
            var mt = "[Ff]ile[Nn]ame=(?<name>.+?);";
            var h = response.Headers["Content-Disposition"];
            h = HttpUtility.UrlDecode(h + ";");
            var mat = Regex.Match(h, mt);
            var oldName = mat.Groups["name"].ToString();
            oldName = oldName.Replace("\"", "");
            var newName = oldName;
            if (newName.IsNullOrEmpty())
            {
                newName = fileName.IsNullOrEmpty() ? Guid.NewGuid() + ".pdf" : fileName;
            }
            var filePath = PathHelper.Combine(dirPath, newName);
            ConsoleLogHelper.WriteLine("生成文件名：" + newName);
            ConsoleLogHelper.WriteLine("生成文件名：" + filePath);
            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }

            //创建本地文件写入流
            Stream stream = new FileStream(filePath, FileMode.Create);
            var bArr = new byte[1024];
            var size = responseStream.Read(bArr, 0, (int)bArr.Length);
            fileSize += size;
            while (size > 0)
            {
                stream.Write(bArr, 0, size);
                size = responseStream.Read(bArr, 0, (int)bArr.Length);
                fileSize += size;
            }
            stream.Close();
            responseStream.Close();
            return filePath;
        }

        #endregion

        #region 同步Post
        /// <summary>
        /// 使用Post方法获取字符串结果，常规提交
        /// </summary>
        /// <returns></returns>
        public static string HttpPost(out HttpStatusCode StatusCode, string url, Dictionary<string, string> formData = null, Encoding encoding = null, int timeOut = 60000, Dictionary<string, string> headers = null)
        {
            StatusCode = HttpStatusCode.OK;
            MemoryStream ms = new MemoryStream();
            formData.FillFormDataStream(ms);//填充formData
            return HttpPost(out StatusCode,url, ms, "application/x-www-form-urlencoded;charset=utf-8", encoding, headers, timeOut);
        }

        /// <summary>
        /// 发送HttpPost请求，使用JSON格式传输数据
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HttpPost(out HttpStatusCode StatusCode, string url, string postData, Encoding encoding = null, Dictionary<string, string> headers = null)
        {
            StatusCode = HttpStatusCode.OK;
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (string.IsNullOrWhiteSpace(postData))
                throw new ArgumentNullException("postData");
            byte[] data = encoding.GetBytes(postData);
            MemoryStream stream = new MemoryStream();
            var formDataBytes = postData == null ? new byte[0] : Encoding.UTF8.GetBytes(postData);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
            return HttpPost(out StatusCode,url, stream, "application/json", encoding, headers);
        }

        /// <summary>
        /// 发送HttpPost请求，使用JSON格式传输数据
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postData"></param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HttpPost(string url, string postData, string ip, Encoding encoding = null, Dictionary<string, string> headers = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (string.IsNullOrWhiteSpace(postData))
                throw new ArgumentNullException("postData");
            byte[] data = encoding.GetBytes(postData);
            MemoryStream stream = new MemoryStream();
            var formDataBytes = postData == null ? new byte[0] : Encoding.UTF8.GetBytes(postData);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
            return HttpPost(url, stream, "application/json", ip, encoding, headers);
        }

        /// <summary>
        /// 使用POST请求数据，使用JSON传输数据
        /// </summary>
        /// <param name="url"></param>
        /// <param name="dataObj">传输对象，转换为JSON传输</param>
        /// <param name="encoding"></param>
        /// <returns></returns>
        public static string HttpPost(out HttpStatusCode StatusCode,string url, object dataObj, Encoding encoding = null, Dictionary<string, string> headers = null)
        {
            StatusCode = HttpStatusCode.OK;
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (dataObj == null)
                throw new ArgumentNullException("dataObj");
            string postData = JsonConvert.SerializeObject(dataObj, new JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
            byte[] data = encoding.GetBytes(postData);
            MemoryStream stream = new MemoryStream();
            var formDataBytes = postData == null ? new byte[0] : Encoding.UTF8.GetBytes(postData);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
            return HttpPost(out StatusCode,url, stream, "application/json", encoding, headers);
        }

        /// <summary>
        /// 使用Post方法获取字符串结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postStream"></param>
        /// <param name="contentType"></param>
        /// <param name="encoding"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static string HttpPost(out HttpStatusCode StatusCode,string url, Stream postStream = null, string contentType = "application/x-www-form-urlencoded", Encoding encoding = null, Dictionary<string, string> headers = null, int timeOut = 60000)
        {
            StatusCode = HttpStatusCode.OK;
            SetCertificatePolicy();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = timeOut;

            request.ContentLength = postStream != null ? postStream.Length : 0;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = false;

            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.87 Safari/537.36 QQBrowser/9.2.5748.400";
            request.ContentType = contentType;

            //request.Headers.Add("Access-Control-Allow-Origin","http://517best.com");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            #region 输入二进制流
            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                Stream requestStream = request.GetRequestStream();

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }

                postStream.Close();//关闭文件访问
            }
            #endregion

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();
            StatusCode = response.StatusCode;
            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = myStreamReader.ReadToEnd();
                    return retString;
                }
            }

        }

       
        /// <summary>
        /// 使用Post方法获取字符串结果
        /// </summary>
        /// <param name="url"></param>
        /// <param name="postStream"></param>
        /// <param name="contentType"></param>
        /// <param name="encoding"></param>
        /// <param name="timeOut"></param>
        /// <returns></returns>
        public static string HttpPost(string url, Stream postStream = null, string contentType = "application/x-www-form-urlencoded", string ip="", Encoding encoding = null, Dictionary<string, string> headers = null, int timeOut = 60000)
        {
            SetCertificatePolicy();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            #region 构建IP

            request.ServicePoint.BindIPEndPointDelegate += (servicePoint, remoteEndPoint, retryCount) =>
            {
                return new IPEndPoint(IPAddress.Parse(ip), 0);
            };

            #endregion

            request.Method = "POST";
            request.Timeout = timeOut;

            request.ContentLength = postStream != null ? postStream.Length : 0;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = false;

            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.87 Safari/537.36 QQBrowser/9.2.5748.400";
            request.ContentType = contentType;

            //request.Headers.Add("Access-Control-Allow-Origin","http://517best.com");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            #region 输入二进制流
            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                Stream requestStream = request.GetRequestStream();

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }

                postStream.Close();//关闭文件访问
            }
            #endregion

            HttpWebResponse response = (HttpWebResponse)request.GetResponse();

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = myStreamReader.ReadToEnd();
                    return retString;
                }
            }

        }
        #endregion

        
        /// <summary>
        /// 多文件上传
        /// </summary>
        /// <param name="url">请求路径</param>
        /// <param name="jsonParam">数据</param>
        /// <param name="files">图片路径</param>
        /// <param name="contentType"></param>
        /// <param name="paramFileName">对应的图片字段</param>
        /// <returns></returns>
        public static string UploadMultiFile(string url, string jsonParam, string[] filePath, string contentType, List<string> fileName)
        {
            string result = "";
            if (filePath.Length <= 0) 
                return result;

            Dictionary<string, string> nvc = new Dictionary<string, string>();
            nvc.Add("queryParam", jsonParam);
            string boundary = "----------------------------" + DateTime.Now.Ticks.ToString("x");//边界符
            byte[] boundarybytes = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "\r\n");//开始边界符
            HttpWebRequest wr = (HttpWebRequest)WebRequest.Create(url);
            wr.ContentType = "multipart/form-data; boundary=" + boundary;
            wr.Method = "POST";
            wr.KeepAlive = true;
            wr.Credentials = System.Net.CredentialCache.DefaultCredentials;
            Stream rs = wr.GetRequestStream();
            string formdataTemplate = "Content-Disposition: form-data; name=\"{0}\"\r\n\r\n{1}";
            foreach (string key in nvc.Keys)
            {
                rs.Write(boundarybytes, 0, boundarybytes.Length);//写入边界符
                string formitem = string.Format(formdataTemplate, key, nvc[key]);
                byte[] formitembytes = System.Text.Encoding.UTF8.GetBytes(formitem);
                rs.Write(formitembytes, 0, formitembytes.Length);//写入数据
            }
            rs.Write(boundarybytes, 0, boundarybytes.Length);//写入边界符

            byte[] trailer = System.Text.Encoding.ASCII.GetBytes("\r\n--" + boundary + "--\r\n");

            for (int k = 0, k2 = filePath.Length; k < k2; k++)
            {
                string fname = fileName[k];
                string headerTemplate = "Content-Disposition: form-data; name=\"{0}\"; filename=\"{1}\"\r\nContent-Type: {2}\r\n\r\n";
                string header = string.Format(headerTemplate, fname, filePath[k], contentType);//写入字符串
                byte[] headerbytes = System.Text.Encoding.UTF8.GetBytes(header);
                rs.Write(headerbytes, 0, headerbytes.Length);//写入文件

                FileStream fileStream = new FileStream(filePath[k], FileMode.Open, FileAccess.Read);
                byte[] buffer = new byte[4096];
                int bytesRead = 0;
                while ((bytesRead = fileStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    rs.Write(buffer, 0, bytesRead);
                }
                fileStream.Close();
                if (k < k2 - 1) rs.Write(boundarybytes, 0, boundarybytes.Length);
            }
            rs.Write(trailer, 0, trailer.Length);
            rs.Close();
            WebResponse wresp = null;
            try
            {
                wresp = wr.GetResponse();
                Stream stream2 = wresp.GetResponseStream();
                StreamReader reader2 = new StreamReader(stream2);
                result = reader2.ReadToEnd();
                return result;
            }
            catch (Exception ex)
            {
                if (wresp != null)
                {
                    wresp.Close();
                    wresp = null;
                }
                result = ex.Message;
            }
            finally
            {
                wr = null;
            }
            return result;
        }

        #endregion

        #region 异步Post
        public static async Task<(HttpStatusCode, string)> HttpPostAsync(string url, Dictionary<string, string> formData = null, Encoding encoding = null, int timeOut = 60000, Dictionary<string, string> headers = null)
        {
            MemoryStream ms = new MemoryStream();
            formData.FillFormDataStream(ms);//填充formData
            return await HttpPostAsync(url, ms, "application/x-www-form-urlencoded;charset=utf-8", encoding, headers, timeOut);
        }
        public static async Task<(HttpStatusCode, string)> HttpPostAsync(string url, string postData, Encoding encoding = null, Dictionary<string, string> headers = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (string.IsNullOrWhiteSpace(postData))
                throw new ArgumentNullException("postData");
            byte[] data = encoding.GetBytes(postData);
            MemoryStream stream = new MemoryStream();
            var formDataBytes = postData == null ? new byte[0] : Encoding.UTF8.GetBytes(postData);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
            return await HttpPostAsync(url, stream, "application/json", encoding, headers);
        }
        public static async Task<(HttpStatusCode, string)> HttpPostAsync(string url, string postData, string ip, Encoding encoding = null, Dictionary<string, string> headers = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (string.IsNullOrWhiteSpace(postData))
                throw new ArgumentNullException("postData");
            byte[] data = encoding.GetBytes(postData);
            MemoryStream stream = new MemoryStream();
            var formDataBytes = postData == null ? new byte[0] : Encoding.UTF8.GetBytes(postData);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
            return await HttpPostAsync(url, stream, "application/json", ip, encoding, headers);
        }
        public static async Task<(HttpStatusCode, string)> HttpPostAsync(string url, Stream postStream = null, string contentType = "application/x-www-form-urlencoded", string ip = "", Encoding encoding = null, Dictionary<string, string> headers = null, int timeOut = 60000)
        {
            SetCertificatePolicy();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);

            #region 构建IP

            request.ServicePoint.BindIPEndPointDelegate += (servicePoint, remoteEndPoint, retryCount) =>
            {
                return new IPEndPoint(IPAddress.Parse(ip), 0);
            };

            #endregion

            request.Method = "POST";
            request.Timeout = timeOut;

            request.ContentLength = postStream != null ? postStream.Length : 0;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = false;

            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.87 Safari/537.36 QQBrowser/9.2.5748.400";
            request.ContentType = contentType;

            //request.Headers.Add("Access-Control-Allow-Origin","http://517best.com");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            #region 输入二进制流
            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                Stream requestStream = await request.GetRequestStreamAsync();

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }

                postStream.Close();//关闭文件访问
            }
            #endregion

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = myStreamReader.ReadToEnd();
                    return (response.StatusCode, retString);
                }
            }

        }
        public static async Task<(HttpStatusCode, string)> HttpPostAsync(string url, object dataObj, Encoding encoding = null, Dictionary<string, string> headers = null)
        {
            if (encoding == null)
                encoding = Encoding.UTF8;
            if (dataObj == null)
                throw new ArgumentNullException("dataObj");
            string postData = JsonConvert.SerializeObject(dataObj, new JsonSerializerSettings() { DateFormatString = "yyyy-MM-dd HH:mm:ss" });
            byte[] data = encoding.GetBytes(postData);
            MemoryStream stream = new MemoryStream();
            var formDataBytes = postData == null ? new byte[0] : Encoding.UTF8.GetBytes(postData);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
            return await HttpPostAsync(url, stream, "application/json", encoding, headers);
        }
        public static async Task<(HttpStatusCode, string)> HttpPostAsync(string url, Stream postStream = null, string contentType = "application/x-www-form-urlencoded", Encoding encoding = null, Dictionary<string, string> headers = null, int timeOut = 60000)
        {
            SetCertificatePolicy();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "POST";
            request.Timeout = timeOut;

            request.ContentLength = postStream != null ? postStream.Length : 0;
            request.Accept = "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8";
            request.KeepAlive = false;

            request.UserAgent = "Mozilla/5.0 (Windows NT 10.0; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/45.0.2454.87 Safari/537.36 QQBrowser/9.2.5748.400";
            request.ContentType = contentType;

            //request.Headers.Add("Access-Control-Allow-Origin","http://517best.com");
            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            #region 输入二进制流
            if (postStream != null)
            {
                postStream.Position = 0;

                //直接写入流
                Stream requestStream = await request.GetRequestStreamAsync();

                byte[] buffer = new byte[1024];
                int bytesRead = 0;
                while ((bytesRead = postStream.Read(buffer, 0, buffer.Length)) != 0)
                {
                    requestStream.Write(buffer, 0, bytesRead);
                }

                postStream.Close();//关闭文件访问
            }
            #endregion

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = await myStreamReader.ReadToEndAsync();
                    return (response.StatusCode, retString);
                }
            }

        }

        #endregion

        

        #region 异步Get
        public static async Task<(HttpStatusCode, string)> HttpGetAsync(string url, Dictionary<string, string> headers = null, Encoding encoding = null, int timeOut = 60000)
        {
            SetCertificatePolicy();
            HttpWebRequest request = (HttpWebRequest)WebRequest.Create(url);
            request.Method = "GET";
            request.Timeout = timeOut;

            if (headers != null)
            {
                foreach (var header in headers)
                {
                    request.Headers.Add(header.Key, header.Value);
                }
            }

            HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync();

            using (Stream responseStream = response.GetResponseStream())
            {
                using (StreamReader myStreamReader = new StreamReader(responseStream, encoding ?? Encoding.GetEncoding("utf-8")))
                {
                    string retString = await myStreamReader.ReadToEndAsync();
                    return (response.StatusCode, retString);
                }
            }
        }
        #endregion
        public static void FillFormDataStream(this Dictionary<string, string> formData, Stream stream)
        {
            string dataString = GetQueryString(formData);
            //ConsoleLogHelper.WriteLine(dataString);
            var formDataBytes = formData == null ? new byte[0] : Encoding.UTF8.GetBytes(dataString);
            stream.Write(formDataBytes, 0, formDataBytes.Length);
            stream.Seek(0, SeekOrigin.Begin);//设置指针读取位置
        }

        /// <summary>
        /// 组装QueryString的方法
        /// 参数之间用&amp;连接，首位没有符号，如：a=1&amp;b=2&amp;c=3
        /// </summary>
        /// <param name="formData"></param>
        /// <returns></returns>
        public static string GetQueryString(this Dictionary<string, string> formData)
        {
            if (formData == null || formData.Count == 0)
            {
                return "";
            }
            StringBuilder sb = new StringBuilder();
            var i = 0;
            foreach (var kv in formData)
            {
                i++;
                sb.AppendFormat("{0}={1}", kv.Key, kv.Value);
                if (i < formData.Count)
                {
                    sb.Append("&");
                }
            }
            return sb.ToString();
        }

        public static string HttpClientPost(string serverUrl, Dictionary<string, string> formData, string fileName, string filePath)
        {
            string fullResponse = "";
            using (HttpClient client = new HttpClient())
            {
                var content = new MultipartFormDataContent();
                foreach (var postParam in formData)
                {
                    content.Add(new StringContent(HttpUtility.UrlEncode(postParam.Value != null ? postParam.Value.ToString() : "")), postParam.Key);
                }
                content.Add(new ByteArrayContent(File.ReadAllBytes(filePath)), "file", HttpUtility.UrlEncode(fileName));
                fullResponse = client.PostAsync(serverUrl, content).Result.Content.ReadAsStringAsync().Result;
            }

            return fullResponse;
        }
    }
}
