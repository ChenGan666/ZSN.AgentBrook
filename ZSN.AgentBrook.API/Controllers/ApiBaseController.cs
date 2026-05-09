using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.Service.Helpers;
using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.PIC;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Service.WebHelpers;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Collections.Generic;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Processing;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiRecoder]
    public class ApiBaseController : CommonApiBaseController
    {
        public CompanyInfo Company { get; set; }
        public ApisettingsInfo setting = null;// SettingsService.Current;
        public MemberSettings memberSetting = null;
        public string dataCode = "";
        public RedisHelper redis = null;
        public ApiBaseController()
        {

            redis = new RedisHelper();
            this.setting = SettingsService.Current;
            this.Company = this.setting != null ? CompanyInfoBussiness.GetModelByAppID(this.setting.AppID) : null;
            if (!this.MemberToken.IsNullOrEmpty())
            {
                var _s = this.MemberSetting;
            }
        }
        public CompanyInfo COMPANY
        {
            get { return this.Company; }
        }

        public MemberSettings MemberSetting
        {
            get
            {
                string _memberid = "";
                int _MemberOtherAuthID = 0;
                string _timestamp = "";
                GetMemberIdByToken(this.MemberToken, out _memberid, out _MemberOtherAuthID, out _timestamp);

                this.memberSetting = SettingsService.GetMemberSetting(_memberid, _MemberOtherAuthID, this.MemberToken);

                return this.memberSetting;
            }
        }

        /// <summary>
        /// 接收加密数据，并解密，返回明文json
        /// </summary>
        public JObject JsonObj
        {
            get
            {
                var Param = this.BodyParams;
                var URL = this.URL;
                var AppKey = "";
                var DataObjStr = "";
                var AccessToken = this.Token;
                JObject DataObj = null;
                try
                {
                    DataObj = JsonConvert.DeserializeObject<JObject>(Param);
                }
                catch (Exception e)
                {
                    return JObject.Parse(JsonConvert.SerializeObject(Json(new { status = -1, msg = "Param Format Error!\n\r" + e.ToString() }).Value));
                }

                string Data = DataObj.JsonGetValue<string>("Data");
                this.dataCode = hashEncrypt.MD5System(URL+Data);


                AppKey = this.memberSetting != null ? this.memberSetting.GetAppKey() : !AccessToken.IsNullOrEmpty() ? AccessToken : COMPANY.SecretKey;
                try
                {
                    DataObjStr = EncryptHelper.AESDecrypt(Data, AppKey);
                }
                catch (Exception e)
                {
                    DataObjStr = "";
                    DefaultLogService.AddOperationLog(1, e.Message);
                }
                

                if (DataObjStr == "")
                {
                    this.dataCode = "";
                    return JObject.Parse(JsonConvert.SerializeObject(Json(new { status = -1, msg = "Decrypt Error!" }).Value));
                }
                else
                {
                    JObject jsonObj = JsonConvert.DeserializeObject<JObject>(DataObjStr);
                    if (jsonObj == null)
                    {
                        this.dataCode = "";
                        return JObject.Parse(JsonConvert.SerializeObject(Json(new { status = -1, msg = "Decrypt Error!Data Empty!" }).Value));
                    }
                    else
                    {
                        DefaultLogService.AddOperationLog(3, JsonConvert.SerializeObject(jsonObj, Formatting.Indented));
                        return jsonObj;
                    }
                }
            }
        }
        /// <summary>
        /// 将结果写入缓存,必须先调用JsonObj
        /// </summary>
        public object CacheWrite
        {
            set
            {
                if (this.dataCode != "")
                {
                    redis.StringSet(this.dataCode,JsonConvert.SerializeObject(value), TimeSpan.FromMilliseconds(ConfigHelper.GetInt("AccessStepTimeOut")));
                }
            }
        }
        /// <summary>
        /// 从缓存中获取请求结果,必须先调用JsonObj
        /// </summary>
        public object CacheValue
        {
            get
            {
                if (this.dataCode != "")
                {
                    object re = null;
                    string _re = redis.StringGet(this.dataCode);
                    if (!_re.IsNullOrEmpty())
                    {
                        try
                        {
                            re = JsonConvert.DeserializeObject<object>(_re);
                        }catch(Exception e)
                        {
                            re = null;
                        }
                    }
                    else
                    {
                        re = null;
                    }

                    return re;
                }
                else
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// UploadFile
        /// </summary>
        /// <param name="formFile"></param>
        /// <returns></returns>
        public static async Task<string> SaveFile(IFormFile formFile)
        {
            string res = "";
            if (formFile != null)
            {
                string FilePath = ConfigHelper.GetString("FilePath");
                string fileMd5 = "";

                using (var stream = formFile.OpenReadStream())
                {
                    using (var md5 = MD5.Create())
                    {
                        var hash = md5.ComputeHash(stream);
                        fileMd5 = BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();

                        FilesInfo fileInfo = FilesInfoBussiness.GetModel(fileMd5);
                        if (fileInfo != null)
                        {
                            res = fileInfo.FileCode;
                        }
                        else
                        {
                            FilePath = FilePath + "/" + DateTime.Now.Year + "/" + DateTime.Now.Month + "/" + DateTime.Now.Day + "/" + DateTime.Now.Hour + "/";
                            fileInfo = new FilesInfo();

                            string ContentType = formFile.ContentType;
                            string _fileExtension = Path.GetExtension(formFile.FileName);
                            if (ContentType == "application/octet-stream")
                            {
                                if (_fileExtension.ToLower().IndexOf("jpg") > -1)
                                {
                                    ContentType = "image/jpeg";
                                }
                                if (_fileExtension.ToLower().IndexOf("png") > -1)
                                {
                                    ContentType = "image/png";
                                }
                                if (_fileExtension.ToLower().IndexOf("gif") > -1)
                                {
                                    ContentType = "image/gif";
                                }
                                if (_fileExtension.ToLower().IndexOf("bmp") > -1)
                                {
                                    ContentType = "image/bmp";
                                }

                            }

                            fileInfo.FileCode = fileMd5;
                            fileInfo.FName = hashEncrypt.MD5System(Guid.NewGuid().ToString()) + "." + Utils.Core.Utils.Utils.GetFileExtensionFromMimeType(ContentType);
                            fileInfo.FFilePath = FilePath;
                            fileInfo.FOriginName = formFile.FileName;
                            fileInfo.FType = ContentType;
                            fileInfo.FAppendTime = DateTime.Now;

                            string savePath = FilePath + fileInfo.FName;
                            if (!Directory.Exists(FilePath))
                            {
                                try
                                {
                                    Directory.CreateDirectory(FilePath);

                                }
                                catch (Exception ex)
                                {
                                    ConsoleHelper.WriteError("CreateDirectory:" + ex.Message);
                                }
                            }
                            try
                            {
                                // 保存文件到指定目录
                                using (var stream_b = new FileStream(savePath, FileMode.CreateNew))
                                {
                                    await formFile.CopyToAsync(stream_b);

                                    if (FilesInfoBussiness.Add(fileInfo) > 0)
                                    {
                                        res = fileMd5;
                                    }
                                }

                            }
                            catch (Exception ex)
                            {
                                ConsoleHelper.WriteError("SaveFile:" + ex.Message);
                            }
                        }
                    }
                }
            }
            return res;
        }

        /// <summary>
        /// Get file
        /// </summary>
        /// <param name="fileCode"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        public static async Task<ByteFile> GetFile(string fileCode, int w = 100, int h = 100)
        {
            ByteFile byteFile = null;

            int _maxWidth = w;           //图片宽度最大限制
            int _maxHeight = h;          //图片高度最大限制

            if (!fileCode.IsNullOrEmpty())
            {
                FilesInfo fileInfo = FilesInfoBussiness.GetModel(fileCode);
                string sourcePath = "";
                if (fileInfo != null)
                {
                    sourcePath = fileInfo.FFilePath + "/" + fileInfo.FName;
                }
                if (!System.IO.File.Exists(sourcePath))
                {
                    fileInfo = new FilesInfo();
                    fileInfo.FType = "png";
                    sourcePath = AppContext.BaseDirectory + "default_file.png";
                }

                if (fileInfo.FType.IndexOf("jpeg") > -1 || fileInfo.FType.IndexOf("png") > -1 || fileInfo.FType.IndexOf("bmp") > -1)
                {
                    string _tempFileName = DateTime.Now.Ticks + ".jpg";
                    using (var image = Image.Load(sourcePath))
                    {
                        // 调整大小
                        int targetWidth = _maxWidth > 0 ? _maxWidth : image.Width;
                        int targetHeight = _maxHeight > 0 ? _maxHeight : image.Height;
                        image.Mutate(x => x.Resize(new ResizeOptions
                        {
                            Size = new Size(targetWidth, targetHeight),
                            Mode = ResizeMode.Max
                        }));

                        using (var ms = new MemoryStream())
                        {
                            image.SaveAsJpeg(ms);
                            byteFile = new ByteFile();
                            byteFile.fileStream = ms.ToArray();
                            byteFile.contentType = "image/jpeg";
                            byteFile.fileName = _tempFileName;
                        }
                    }
                }
                else
                {
                    // 读取文件内容到字节数组
                    byte[] fileBytes;
                    using (var stream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read))
                    {
                        fileBytes = new byte[stream.Length];
                        await stream.ReadAsync(fileBytes, 0, fileBytes.Length);
                        byteFile = new ByteFile();
                        byteFile.fileStream = fileBytes;
                        byteFile.contentType = fileInfo.FType;
                        byteFile.fileName = fileInfo.FName;
                    }
                }

            }
            return byteFile;
        }
        public static List<string> FormatFileCode(string picCode) {
            List<string> _re = new List<string>();

            string[] _picarr = (picCode+",").Split(',');
            foreach(string pic in _picarr)
            {
                if (pic != "")
                {
                    _re.Add(string.Format(ConfigHelper.GetString("previewHost"), pic.Trim()));
                }
            }
            return _re;
        } 
    }
}
