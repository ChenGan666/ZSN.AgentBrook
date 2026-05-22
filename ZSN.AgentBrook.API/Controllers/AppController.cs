using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using ZSN.AI.Service.Controllers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Public")]
    [Route("api/[controller]/[action]")]
    public class AppController: ApiBaseController
    {
        public AppController()
        {

        }

        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }
        /// <summary>
        /// 获取应用列表
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = false)]
        public JsonMsg<List<AppInfo>> GetList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                List<AppInfo> _list = AppInfoBussiness.GetList(" SystemStatus=2 ");

                foreach (var item in _list)
                {
                    item.AICON = string.Format(ConfigHelper.GetString("previewHost"), item.AICON);
                }

                return JsonMsg<List<AppInfo>>.OK(_list);
            }
            else
            {
                return JsonMsg<List<AppInfo>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<AppInfo> Get([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string _AppID = jObject.JsonGetValue<string>("AppID", "");
                if (!_AppID.IsNullOrEmpty())
                {
                    AppInfo _app = AppInfoBussiness.GetModel(_AppID);

                    return JsonMsg<AppInfo>.OK(_app);
                }
                else
                {
                    return JsonMsg<AppInfo>.Error(null, ErrorCode.DataEmpty);
                }
            }
            else
            {
                return JsonMsg<AppInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }
    }
}
