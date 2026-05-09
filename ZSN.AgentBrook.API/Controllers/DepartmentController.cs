using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using ZSN.AgentBrook.API.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Attributes;
using ZSN.AI.Service.Controllers;
using ZSN.Utils.Core.Extensions;

namespace ZSN.AgentBrook.API.Controllers
{
    [ApiController]
    [ApiExplorerSettings(GroupName = "V1-Public")]
    [Route("api/[controller]/[action]")]
    public class DepartmentController : ApiBaseController
    {
        public DepartmentController()
        {
        }

        [HiddenApi]
        [HttpGet]
        public IActionResult Index()
        {
            return BuildSuccessResult(new { msg = "success" });
        }

        /// <summary>
        /// 获取部门列表信息
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = false)]
        public JsonMsg<List<DepartmentInfo>> GetList([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                List<DepartmentInfo> _list = DepartmentInfoBussiness.GetList();

                return JsonMsg<List<DepartmentInfo>>.OK(_list);
            }
            else
            {
                return JsonMsg<List<DepartmentInfo>>.Error(null, ErrorCode.DataFormatError);
            }
        }

        [ApiExplorerSettings(GroupName = "V1-Public")]
        [HttpPost]
        [Consumes("application/json")]
        [APIChecker(Token = false)]
        public JsonMsg<DepartmentInfo> Get([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int DepartmentID = jObject.JsonGetValue<int>("DepartmentID",0);
                if (DepartmentID > 0)
                {
                    DepartmentInfo _list = DepartmentInfoBussiness.GetModel(DepartmentID);

                    return JsonMsg<DepartmentInfo>.OK(_list);
                }
                else
                {
                    return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.DataEmpty);
                }
            }
            else
            {
                return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 部门（新建、更新）
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<DepartmentInfo> Save([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int DepartmentID = jObject.JsonGetValue<int>("DepartmentID", 0);
                string dName = jObject.JsonGetValue<string>("dName");
                string dInfo = jObject.JsonGetValue<string>("dInfo");
                int dState = jObject.JsonGetValue<int>("dState", 0);

                DepartmentInfo _dp = new DepartmentInfo();
                if (DepartmentID > 0)
                {
                    _dp = DepartmentInfoBussiness.GetModel(DepartmentID);

                    _dp.DName = dName;
                    _dp.DInfo = dInfo;
                    _dp.DState = dState;

                    if (DepartmentInfoBussiness.Update(_dp))
                    {
                        return JsonMsg<DepartmentInfo>.OK(_dp);
                    }
                    else
                    {
                        return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.ServerError);
                    }
                }
                else
                {
                    _dp.DName = dName;
                    _dp.DInfo = dInfo;
                    _dp.DState = dState;
                    _dp.DAppendtime = DateTime.Now;

                    _dp.DepartmentID = DepartmentInfoBussiness.Add(_dp);

                    if (_dp.DepartmentID > 0)
                    {
                        return JsonMsg<DepartmentInfo>.OK(_dp);
                    }
                    else
                    {
                        return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.ServerError);
                    }

                }
            }
            else
            {
                return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 更新部门状态
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<DepartmentInfo> State([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int DepartmentID = jObject.JsonGetValue<int>("DepartmentID", 0);
                int dState = jObject.JsonGetValue<int>("dState", 0);

                DepartmentInfo _dp = new DepartmentInfo();
                if (DepartmentID > 0)
                {
                    _dp = DepartmentInfoBussiness.GetModel(DepartmentID);

                    _dp.DState = dState;

                    if (DepartmentInfoBussiness.Update(_dp))
                    {
                        return JsonMsg<DepartmentInfo>.OK(_dp);
                    }
                    else
                    {
                        return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.ServerError);
                    }
                }
                else
                {
                    return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.DataFormatError);
                }
            }
            else
            {
                return JsonMsg<DepartmentInfo>.Error(null, ErrorCode.DataFormatError);
            }
        }

        /// <summary>
        /// 删除部门
        /// </summary>
        /// <param name="paramValue"></param>
        /// <returns></returns>
        [ApiExplorerSettings(GroupName = "V1-Manage")]
        [HttpPost]
        [Consumes("application/json")]
        [MemberCheck(Token = true, MemberToken = true)]
        public JsonMsg<string> Delete([FromBody] PostData paramValue)
        {
            JObject jObject = this.JsonObj;
            if (jObject.JsonGetValue<int>("status") != -1)
            {
                int DepartmentID = jObject.JsonGetValue<int>("DepartmentID", 0);

                if (DepartmentID > 0)
                {

                    if (DepartmentInfoBussiness.Delete(DepartmentID))
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

    }
}
