using ZSN.AgentBrook.API.Attributes;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZSN.Utils.Core.Helpers;
using ZSN.Utils.Core.Extensions;
using FileInfo = ZSN.AI.Entity.FileInfo;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-User")]
    [Route("api/[controller]/[action]")]
    public class FileController: ApiBaseController
    {
        /// <summary>
        /// Upload file
        /// </summary>
        /// <param name="paramValue">
        /// PostData.Data = {"FileMd5":"filemd5"}
        /// </param>
        /// <returns></returns>
        [HttpPost]
        [Consumes("multipart/form-data")]
        [MemberCheck]
        public async Task<JsonMsg<FileInfo>> Upload([FromForm] string Data) 
        {
            JObject jObject = this.JsonObj;
            if (jObject.Value<int>("status") != -1)
            {
                var Files = this.PostFile;
                string FileCode = "";
                if (Files != null)
                {
                    string FileMd5 = jObject.Value<string>("FileMd5").SecureSQL();
                    string[] filemd5List = FileMd5.Split(",");

                    List<string> resList = new List<string>();
                    IFormFile formFile = null;
                    for (int i = 0; i < Files.Count; i++)
                    {
                        formFile = Files[i];
                        resList.Add(await SaveFile(formFile));
                    }

                    FileCode = string.Join(",", resList);
                }
                FileInfo file = new FileInfo { FileCode = FileCode, Url = string.Format(ConfigHelper.GetString("previewHost"), FileCode) };
                return JsonMsg<FileInfo>.OK(file);
            }
            else
            {
                return JsonMsg<FileInfo>.Error(null,ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// GetFile
        /// </summary>
        /// <param name="fileCode"></param>
        /// <param name="w"></param>
        /// <param name="h"></param>
        /// <returns></returns>
        [HttpGet]
        [ApiRecoder(IsGetFile = true)]
        [MemberCheck(MemberToken = false, Token = false, Sign = false, Timestamp = false)]
        public async Task<IActionResult> Get(string fileCode, int w = 0, int h = 0)
        {
            if (fileCode.IsNullOrEmpty())
            {
                fileCode = "x";
            }
                ByteFile byteFile = await GetFile(fileCode.SecureSQL(), w, h);
                if (byteFile != null)
                {
                    System.Net.Mime.ContentDisposition cd = new System.Net.Mime.ContentDisposition
                    {
                        FileName = byteFile.fileName,
                        Inline = true  // false = prompt the user for downloading;  true = browser to try to show the file inline
                    };
                    Response.Headers.Add("Content-Disposition", cd.ToString());
                    Response.Headers.Add("X-Content-Type-Options", "nosniff");
                    return File(byteFile.fileStream, byteFile.contentType);
                }
                else
                {
                    return CommonApiBaseController.GetErrorResult(ErrorCode.DataEmpty);
                }
           
        }
    }
}
