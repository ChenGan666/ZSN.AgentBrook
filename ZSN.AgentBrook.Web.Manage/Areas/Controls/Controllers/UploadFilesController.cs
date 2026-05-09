using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.WebHelpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.StaticFiles;
using ZSN.AI.Web.Areas.Controls.Controllers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.Service.Common;
using System.Runtime;
using System.Security.Cryptography;
using ZSN.Utils.Core.Utils;
using Tools = ZSN.AI.Service.Common.Tools;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    public class UploadFilesController : ControlsBaseController
    {

        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="paramValue">
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [AdminAttributes]
        public async Task<IActionResult> Save([FromForm] string Data)
        {
            if (!Tools.CheckLogin(false))
            {
                return BuildFailResult();
            }

            var Files = HttpContextHelper.Current.Request.Form.Files;
            string FileCode = "";
            if (Files != null)
            {
                List<string> resList = new List<string>();
                IFormFile formFile = null;
                for (int i = 0; i < Files.Count; i++)
                {
                    formFile = Files[i];
                    resList.Add(await SaveFile(formFile));
                }

                FileCode = string.Join(",", resList);
            }

            return BuildSuccessResult(new { FileCode = FileCode });

        }

        public string GetFileExt(string name)
        {
            return Path.GetExtension(name);
        }
        /// <summary>
		/// UploadFile
		/// </summary>
		/// <param name="formFile"></param>
		/// <returns></returns>
		private static async Task<string> SaveFile(IFormFile formFile)
        {
            HashEncrypt hashEncrypt = new HashEncrypt();
            string res = "";
            if (formFile != null)
            {
                if (!ConfigHelper.GetString("UploadFileType").Contains(Path.GetExtension(formFile.FileName).TrimStart('.'), StringComparison.CurrentCultureIgnoreCase))
                {
                    res = "error file type!";
                }
                else
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
                                fileInfo.FileCode = fileMd5;
                                fileInfo.FName = hashEncrypt.MD5System(Guid.NewGuid().ToString()) + "." + Utils.Core.Utils.Utils.GetFileExtensionFromMimeType(formFile.ContentType);
                                fileInfo.FFilePath = FilePath;
                                fileInfo.FOriginName = formFile.FileName;
                                fileInfo.FType = formFile.ContentType;
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

            }
            return res;
        }
    }
}
