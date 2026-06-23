using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using ZSN.Utils.Core.Extensions;

namespace ZSN.Utils.Core.Helpers
{
    public static class QiniuHelper
    {
        /// <summary>
        ///     上传文件到七牛的CDN，需要配置QINIU_AccessKey及QINIU_SecretKey
        /// </summary>
        /// <param name="uploadPath">上传到服务器上的文件名，可以带子目录，如：pics/test.jpg</param>
        /// <param name="nameSpace">对应七牛上面的空间名字，如 xx-static</param>
        /// <param name="fileData">文件数据内容</param>
        /// <param name="saveAsMp3File">转换成mp3格式文件位置</param>
        /// <returns></returns>
        public static string UploadQiniuFile(string uploadPath, string nameSpace, byte[] fileData, string saveAsMp3File = null)
        {
            var accessKey = ConfigHelper.GetString("QINIU_AccessKey");
            var secretKey = ConfigHelper.GetString("QINIU_SecretKey");
            var uiStamp = DateTime.Now.AddHours(1).ToUnixStamp();
            var persistentOps = string.Empty;
            if (!string.IsNullOrEmpty(saveAsMp3File))
            {
                //视频的格式："avthumb/mp4/s/1280x720/vb/1.25m|saveas/" 
                persistentOps = "avthumb/mp3/ab/64k|saveas/" + (nameSpace + ":" + saveAsMp3File).ToBase64SafeUrl();
            }
            var putPolicy =
                string.Format(
                    @"{{""scope"":""{1}:{2}"",""deadline"":{0}, ""persistentPipeline"": ""weiquanvoice"", ""persistentOps"": ""{3}"",""returnBody"":""{{\""name\"":$(fname),\""size\"":$(fsize),\""w\"":$(imageInfo.width),\""h\"":$(imageInfo.height),\""hash\"":$(etag)}}""}}"
                    , uiStamp
                    , nameSpace
                    , uploadPath
                    , persistentOps
                    );
            var encodedPutPolicy = putPolicy.ToBase64SafeUrl();
            var encodedSign = EncryptHelper.HmacSha1Sign(encodedPutPolicy, secretKey).ToBase64SafeUrl();
            var uploadToken = accessKey + ':' + encodedSign + ':' + encodedPutPolicy;

            //构造multipart/form-data
            var sbContent = new StringBuilder();
            sbContent.AppendLine("--SPLITLINE");
            sbContent.AppendLine("Content-Disposition: form-data; name=\"token\"");
            sbContent.AppendLine();
            sbContent.AppendLine(uploadToken);

            sbContent.AppendLine("--SPLITLINE");
            sbContent.AppendLine("Content-Disposition: form-data; name=\"key\"");
            sbContent.AppendLine();
            sbContent.AppendLine(uploadPath);

            sbContent.AppendLine("--SPLITLINE");
            sbContent.AppendLine("Content-Disposition: form-data; name=\"file\"; filename=\"" + uploadPath + "\"");
            sbContent.AppendLine("Content-Type: application/octet-stream");
            sbContent.AppendLine("Content-Transfer-Encoding: binary");
            sbContent.AppendLine();

            var buffer = new List<byte>();
            buffer.AddRange(Encoding.UTF8.GetBytes(sbContent.ToString()));
            buffer.AddRange(fileData);
            buffer.AddRange(Encoding.UTF8.GetBytes("\r\n--SPLITLINE--"));

            using (var client = new WebClient())
            {
                client.Headers.Add("Content-Type", "multipart/form-data; boundary=SPLITLINE");
                var result = Encoding.UTF8.GetString(client.UploadData("http://upload.qiniu.com", buffer.ToArray()));
                if (result.ToLower().Contains("error"))
                {
                    throw new ApplicationException("上传失败：" + result);
                }
                return $"http://{nameSpace}.qiniudn.com/{(string.IsNullOrEmpty(saveAsMp3File) ? uploadPath : saveAsMp3File)}";
            }
        }

        /// <summary>
        ///     将微信的资源转换为7牛资源
        /// </summary>
        /// <param name="url"></param>
        /// <param name="style"></param>
        /// <returns></returns>
        public static string WeiXinResUrlToQiniuUrl(string url, string style = "")
        {
            switch (GetDomian(url))
            {
                case "mmsns.qpic.cn":
                    return url.Replace("mmsns.qpic.cn", "abc.domain.com").Replace("https://", "http://") + style;
                default:
                    return url;
            }
        }

        private static string GetDomian(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return "";
            }

            var s1 = url.IndexOf("//");
            var s2 = url.IndexOf("/", s1 + 2);
            var s = url.Substring(s1 + 2, s2 - s1 - 2);
            return s;
        }
    }
}