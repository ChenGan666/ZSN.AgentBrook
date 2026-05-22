
using ZSN.AI.LLMServer.Controllers;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using System.Net;
using ZSN.Utils.Core.Extensions;
using ZSN.AI.Service.Attributes;
using ZSN.AgentBrook.API.Controllers;
using ZSN.AgentBrook.API.Attributes;
using System.Diagnostics;
using ZSN.AI.API.Pages;
using ZSN.AI.Entity.KnowledgeBase;
using System.IO;

namespace ZSN.AI.LLMServer.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Public")]
    [Route("api/[controller]/[action]")]
    public class KnowledgeBaseController : ApiBaseController
    {
        public KnowledgeBaseController() {
        }

        /// <summary>
        /// 获取知识库图片
        /// </summary>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpGet]
        [ApiRecoder(IsGetFile = true)]
        [MemberCheck(MemberToken = false, Token = false, Sign = false, Timestamp = false)]
        public async Task<IActionResult> GetImage(string imageId)
        {
            if (string.IsNullOrEmpty(imageId))
                return NotFound();

            var imageInfo = DocumentImageBusiness.GetByImageId(imageId);
            if (imageInfo == null || string.IsNullOrEmpty(imageInfo.StoragePath))
                return NotFound();

            // StoragePath是相对路径，需要拼接ImageRootPath得到绝对路径
            var rootPath = ConfigHelper.GetString("KnowledgeBase:ImageRootPath");
            var absolutePath = Path.IsPathRooted(imageInfo.StoragePath)
                ? imageInfo.StoragePath
                : Path.Combine(rootPath, imageInfo.StoragePath.Replace("./", "").Replace(".\\", ""));

            if (!System.IO.File.Exists(absolutePath))
                return NotFound();

            var contentType = !string.IsNullOrEmpty(imageInfo.MimeType) ? imageInfo.MimeType : "image/png";
            var fileBytes = await System.IO.File.ReadAllBytesAsync(absolutePath);

            Response.Headers.Add("Content-Disposition", $"inline; filename=\"{imageInfo.ImageId}\"");
            Response.Headers.Add("X-Content-Type-Options", "nosniff");
            return File(fileBytes, contentType);
        }

        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        /// <summary>
        /// 获取知识库
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<KnowledgeBaseInfo> Get([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string KnowledgeBaseID = jObject.JsonGetValue<string>("KnowledgeBaseID", "");

                if (!KnowledgeBaseID.IsNullOrEmpty())
                {
                    KnowledgeBaseInfo _kb = KnowledgeBaseInfoBussiness.GetModel(KnowledgeBaseID);

                    return JsonMsg<KnowledgeBaseInfo>.OK(_kb);
                }
                else
                {
                    return JsonMsg<KnowledgeBaseInfo>.Error(null, ErrorCode.DataEmpty);
                }
            }
            else
            {
                return JsonMsg<KnowledgeBaseInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 获取知识库列表
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<PageData<List<KnowledgeBaseInfo>>> GetList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string _sql = " SystemStatus = 1 and (MemberID='system' or MemberID='"+memberSetting.FullMember.Member.MemberID+"') ";
                int TagClassID = jObject.JsonGetValue<int>("TagClassID", 0);
                int KnowledgeBaseTagID = jObject.JsonGetValue<int>("KnowledgeBaseTagID", 0);

                int index = jObject.JsonGetValue<int>("Index", 1);
                int size = jObject.JsonGetValue<int>("Size", 10);
                string KeyWord = jObject.JsonGetValue<string>("KeyWord", "");

                PageData<List<KnowledgeBaseInfo>> _Data = new PageData<List<KnowledgeBaseInfo>>();

                List<KnowledgeBaseTagInfo> _allTag = new List<KnowledgeBaseTagInfo>();
                if (TagClassID > 0)
                {
                    _allTag = KnowledgeBaseTagInfoBussiness.GetAllTagByTagClassID(TagClassID);
                }

                if (KnowledgeBaseTagID > 0)
                {
                    _allTag.Add(new KnowledgeBaseTagInfo() { KnowledgeBaseTagID = KnowledgeBaseTagID });
                }

                if(_allTag.Count > 0)
                {
                    var conditions = _allTag.Select(Dic =>
                         $"DicIDList LIKE '{Dic.KnowledgeBaseTagID},%' OR DicIDList LIKE '%,{Dic.KnowledgeBaseTagID},%' OR DicIDList LIKE '%,{Dic.KnowledgeBaseTagID}' OR DicIDList = '{Dic.KnowledgeBaseTagID}'"
                    );
                    var whereClause = string.Join(" OR ", conditions);
                    _sql += " and (" + whereClause + ")";
                }
                else
                {
                    _sql += " and (DicIDList is null or DicIDList = '')";
                }
                if (!KeyWord.IsNullOrEmpty())
                {
                    _sql += " and (Name like '%" + KeyWord + "%' or Description like '%" + KeyWord + "%')";
                }

                List<KnowledgeBaseInfo> _list = KnowledgeBaseInfoBussiness.GetListByPage(size, index, _sql, out int pagetotal, out int total, 1,   "*", "LastUpdateTime");

                _Data.Data = _list;
                _Data.pagetotal = pagetotal;
                _Data.total = total;

                return JsonMsg<PageData<List<KnowledgeBaseInfo>>>.OK(_Data);
            }
            else
            {
                return JsonMsg<PageData<List<KnowledgeBaseInfo>>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 获取知识库标签列表
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<List<KnowledgeBaseTagInfo>> GetTagList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                List<KnowledgeBaseTagInfo> _allTag = new List<KnowledgeBaseTagInfo>();
                int TagClassID = jObject.JsonGetValue<int>("TagClassID", 0);
                if (TagClassID > 0)
                {
                    _allTag = KnowledgeBaseTagInfoBussiness.GetAllTagByTagClassID(TagClassID);
                }
                else
                {
                    //取个人知识库标签，从知识库中获取

                }

                return JsonMsg<List<KnowledgeBaseTagInfo>>.OK(_allTag);
            }
            else
            {
                return JsonMsg<List<KnowledgeBaseTagInfo>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 获取知识库文件列表
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<PageData<List<KnowledgeBaseFileInfo>>> GetFileList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string _sql = " 1=1 ";
                string KnowledgeBaseID = jObject.JsonGetValue<string>("KnowledgeBaseID", "");

                int index = jObject.JsonGetValue<int>("Index", 1);
                int size = jObject.JsonGetValue<int>("Size", 10);
                string KeyWord = jObject.JsonGetValue<string>("KeyWord", "");

                PageData<List<KnowledgeBaseFileInfo>> _Data = new PageData<List<KnowledgeBaseFileInfo>>();

                if (!KnowledgeBaseID.IsNullOrEmpty())
                {
                    _sql += " and KnowledgeBaseID="+ KnowledgeBaseID;
                }
                List<KnowledgeBaseFileInfo> _list = KnowledgeBaseFileInfoBussiness.GetListByPage(size, index, _sql, out int pagetotal, out int total, 1, "*", "LastUpdateTime");

                _Data.Data = _list;
                _Data.pagetotal = pagetotal;
                _Data.total = total;

                return JsonMsg<PageData<List<KnowledgeBaseFileInfo>>>.OK(_Data);
            }
            else
            {
                return JsonMsg<PageData<List<KnowledgeBaseFileInfo>>>.Error(null, ErrorCode.DataFormatError);
            }
        }
    }
}
