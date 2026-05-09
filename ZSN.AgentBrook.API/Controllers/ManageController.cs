using Lucene.Net.Util.Automaton;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Runtime.Intrinsics.Arm;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Manage")]
    [Route("api/[controller]/[action]")]
    public class ManageController: ApiBaseController
    {
        public ManageController() { 
        
        }
        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        #region Company
        [ApiExplorerSettings(GroupName = "V1-Base")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<CompanyInfo> CompanySave([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                string cFullName = jObject.JsonGetValue<string>("cFullName");
                string cTitle = jObject.JsonGetValue<string>("cTitle");
                string cIDCode = jObject.JsonGetValue<string>("cIDCode");
                string cCity = jObject.JsonGetValue<string>("cCity");
                string cScale = jObject.JsonGetValue<string>("cScale");
                string cInfo = jObject.JsonGetValue<string>("cInfo");
                string cLogo = jObject.JsonGetValue<string>("cLogo");


                CompanyInfo _ci = CompanyInfoBussiness.GetModel();
                if (_ci != null)
                {
                    _ci.CFullName = cFullName;
                    _ci.CTitle = cTitle;
                    _ci.CIDCode = cIDCode;
                    _ci.CCity = cCity;
                    _ci.CScale = cScale;
                    _ci.CInfo = cInfo;
                    _ci.CLogo = cLogo;
                    if (CompanyInfoBussiness.Update(_ci))
                    {
                        return JsonMsg<CompanyInfo>.OK(_ci);
                    }
                    else
                    {
                        return JsonMsg<CompanyInfo>.Error(null, ErrorCode.ServerError);
                    }
                }
                else
                {
                    _ci = new CompanyInfo();
                    _ci.CFullName = cFullName;
                    _ci.CTitle = cTitle;
                    _ci.CIDCode = cIDCode;
                    _ci.CCity = cCity;
                    _ci.CScale = cScale;
                    _ci.CInfo = cInfo;
                    _ci.CLogo = cLogo;

                    _ci.CompanyID = CompanyInfoBussiness.Add(_ci);
                    if (_ci.CompanyID > 0)
                    {
                        return JsonMsg<CompanyInfo>.OK(_ci);
                    }
                    else
                    {
                        return JsonMsg<CompanyInfo>.Error(null, ErrorCode.ServerError);
                    }
                }
                
            }
            else
            {
                return JsonMsg<CompanyInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        #endregion


    }
}
