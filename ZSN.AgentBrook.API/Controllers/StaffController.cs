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
    public class StaffController: ApiBaseController
    {
        public StaffController() { 
        
        }
        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        #region Staff
        /// <summary>
        /// 员工（新建、更新）
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<StaffInfo> Save([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int StaffID = jObject.JsonGetValue<int>("StaffID", 0);
                string sCode = jObject.JsonGetValue<string>("sCode");
                string sName = jObject.JsonGetValue<string>("sName");
                string sTitle = jObject.JsonGetValue<string>("sTitle");
                string dName = jObject.JsonGetValue<string>("dName");
                DateTime sEntryTime = jObject.JsonGetValue<DateTime>("sEntryTime", DateTime.Now);
                int DepartmentID = jObject.JsonGetValue<int>("DepartmentID", 0);
                int sState = jObject.JsonGetValue<int>("sState", 0);
                string sEmail = jObject.JsonGetValue<string>("sEmail");
                string sPhone = jObject.JsonGetValue<string>("sPhone");
                string MemberID = jObject.JsonGetValue<string>("MemberID");

                StaffInfo _si = new StaffInfo();
                if (StaffID > 0)
                {
                    _si = StaffInfoBussiness.GetModel(StaffID);

                    _si.SCode = sCode;
                    _si.SName = sName;
                    _si.STitle = sTitle;
                    _si.DepartmentID = DepartmentID;
                    _si.DName = dName;
                    _si.SEntryTime = sEntryTime;
                    _si.SState = sState;
                    _si.SAppendTime = DateTime.Now;
                    _si.SEmail = sEmail;
                    _si.SPhone = sPhone;
                    _si.MemberID = MemberID;
                    ;
                    if (StaffInfoBussiness.Update(_si))
                    {
                        return JsonMsg<StaffInfo>.OK(_si);
                    }
                    else
                    {
                        return JsonMsg<StaffInfo>.Error(null, ErrorCode.ServerError);
                    }
                }
                else
                {
                    _si.SCode = sCode;
                    _si.SName = sName;
                    _si.STitle = sTitle;
                    _si.DepartmentID = DepartmentID;
                    _si.DName = dName;
                    _si.SEntryTime = sEntryTime;
                    _si.SState = sState;
                    _si.SAppendTime = DateTime.Now;
                    _si.SEmail = sEmail;
                    _si.SPhone = sPhone;
                    _si.MemberID = MemberID;

                    _si.StaffID = StaffInfoBussiness.Add(_si);

                    if (_si.StaffID > 0)
                    {
                        return JsonMsg<StaffInfo>.OK(_si);
                    }
                    else
                    {
                        return JsonMsg<StaffInfo>.Error(null, ErrorCode.ServerError);
                    }

                }
            }
            else
            {
                return JsonMsg<StaffInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 获取员工列表
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = false)]
        public JsonMsg<List<StaffInfo>> GetList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                List<StaffInfo> _list = StaffInfoBussiness.GetList();

                return JsonMsg<List<StaffInfo>>.OK(_list);
            }
            else
            {
                return JsonMsg<List<StaffInfo>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 获取员工
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = false)]
        public JsonMsg<StaffInfo> Get([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int StaffID = jObject.JsonGetValue<int>("StaffID", 0);
                if (StaffID > 0)
                {
                    StaffInfo _list = StaffInfoBussiness.GetModel(StaffID);

                    return JsonMsg<StaffInfo>.OK(_list);
                }
                else
                {
                    return JsonMsg<StaffInfo>.Error(null, ErrorCode.DataEmpty);
                }
            }
            else
            {
                return JsonMsg<StaffInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 更新员工状态
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<StaffInfo> State([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int StaffID = jObject.JsonGetValue<int>("StaffID", 0);
                int sState = jObject.JsonGetValue<int>("sState", 0);

                StaffInfo _si = new StaffInfo();
                if (StaffID > 0)
                {
                    _si = StaffInfoBussiness.GetModel(StaffID);

                    _si.SState = sState;

                    if (StaffInfoBussiness.Update(_si))
                    {
                        return JsonMsg<StaffInfo>.OK(_si);
                    }
                    else
                    {
                        return JsonMsg<StaffInfo>.Error(null, ErrorCode.ServerError);
                    }
                }
                else
                {
                    return JsonMsg<StaffInfo>.Error(null, ErrorCode.DataFormatError);
                }
            }
            else
            {
                return JsonMsg<StaffInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 删除员工
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker]
        public JsonMsg<string> Delete([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int StaffID = jObject.JsonGetValue<int>("StaffID", 0);

                if (StaffID > 0)
                {

                    if (StaffInfoBussiness.Delete(StaffID))
                    {
                        return JsonMsg<string>.OK("删除成功");
                    }
                    else
                    {
                        return JsonMsg<string>.Error(null, ErrorCode.ServerError);
                    }
                }
                else
                {
                    return JsonMsg<string>.Error(null, ErrorCode.DataFormatError);
                }
            }
            else
            {
                return JsonMsg<string>.Error(null, ErrorCode.DataFormatError);
            }
        }

        #endregion


    }
}
