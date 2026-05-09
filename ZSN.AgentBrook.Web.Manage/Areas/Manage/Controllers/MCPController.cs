using ZSN.AI.BLL;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using Newtonsoft.Json;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class MCPController: AdminBaseController
    {
        public IActionResult Index(int index = 1, int size = 10)
        {
            var lst = McpInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.MCPList = lst;
            return View();
        }
        [HttpPost]
        public JsonMsg<string> MCPStatus(string mid, bool status)
        {
            var mcp = McpInfoBussiness.GetModel(mid);
            mcp.SystemStatus = status ? ZSN.AI.Entity.McpState.Normal : ZSN.AI.Entity.McpState.Disabled;
            McpInfoBussiness.Update(mcp);
            return JsonMsg<string>.OK("更新成功");
        }
        public IActionResult Edit(string mid)
        {
            var mcp = mid.IsNullOrEmpty() ? new McpInfo() : McpInfoBussiness.GetModel(mid);

            if (mid.IsNullOrEmpty())
            {
                mcp.Config = JsonConvert.SerializeObject(new MCPConfig());
                mcp.OutputConfig = new List<Output>() { new Output() };
            }

            ViewBag.MCP = mcp;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            return View();
        }
        [HttpPost]
        public JsonMsg<string> MCPSave(McpInfo mcp,string OutputConfig)
        {
            if (!OutputConfig.IsNullOrEmpty())
            {
                try
                {
                    mcp.OutputConfig = JsonConvert.DeserializeObject<List<Output>>(OutputConfig);
                }catch(Exception ex)
                {
                    return JsonMsg<string>.Error("OutputConfig格式错误",ErrorCode.DataFormatError);
                }
            }

            if (mcp.MCPID.IsNullOrEmpty())
            {
                mcp.MCPID = Guid.NewGuid().ToString();
                mcp.CreateTime = DateTime.Now;
                McpInfoBussiness.Add(mcp);
            }
            else
            {
                McpInfoBussiness.Update(mcp);
            }

            return JsonMsg<string>.OK("保存成功");
        }
        public JsonMsg<string> MCPDel(string mid)
        {
            McpInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
