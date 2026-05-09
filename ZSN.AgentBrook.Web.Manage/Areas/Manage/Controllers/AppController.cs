using Microsoft.AspNetCore.Mvc;
using MySqlX.XDevAPI;
using Newtonsoft.Json;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Chat;
using ZSN.AI.Entity.Model.Enum;
using ZSN.AI.Node;
using ZSN.AI.Service.Controllers;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class AppController:AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = AppInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.AppList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> AppStatus(string mid, bool status)
        {
            var App = AppInfoBussiness.GetModel(mid);
            App.SystemStatus = status ? ZSN.AI.Entity.AppStatus.Running : ZSN.AI.Entity.AppStatus.Stop;

            //针对应用工作流中的Timer节点进行处理
            string AppID = App.AppID;
            WorkflowInfo workflowInfo = WorkflowInfoBussiness.GetModelByAppID(AppID);
            if (workflowInfo != null)
            {
                string WorkflowID = workflowInfo.WorkflowID;
                //查找激活的时间触发节点
                List<WorkflowNodeInfo> nodes = WorkflowNodeInfoBussiness.GetListByNodeType(NodeType.TimeTrigger, workflowInfo.WorkflowID);
                foreach (WorkflowNodeInfo node in nodes)
                {
                    NodeConfig nodeConfig = JsonConvert.DeserializeObject<NodeConfig>(node.Config.ToString());
                    if (nodeConfig?.data != null)
                    {
                        if (App.SystemStatus == ZSN.AI.Entity.AppStatus.Running)
                        {
                            TimeTriggerData timeTriggerData = JsonConvert.DeserializeObject<TimeTriggerData>(nodeConfig.data.ToString());
                            if(timeTriggerData != null && timeTriggerData.enable)
                            {
                                string SessionID = Guid.NewGuid().ToString();
                                string ProcessesID = Guid.NewGuid().ToString();
                                string nodeId = node.NodeID;
                                string MemberID = "System";
                                List<Inputs> inputs = new List<Inputs>();
                                string FromTaskID = "";
                                string FromMainTaskID = "";
                                string AgentNodeID = "";

                                Execution.ExecutionNode(AppID, SessionID, ProcessesID, nodeId, MemberID, "TimeTrigger", inputs, FromTaskID, FromMainTaskID, AgentNodeID, WorkflowID);
                            }
                            else
                            {
                                System.Collections.Generic.List<TaskInfo> tasks = TaskInfoBussiness.GetList(NodeType.TimeTrigger, WorkflowID);
                                foreach (var task in tasks)
                                {
                                    TaskInfoBussiness.Delete(task.TaskID);
                                }
                            }
                            
                        }
                        if (App.SystemStatus == ZSN.AI.Entity.AppStatus.Stop)
                        {
                            System.Collections.Generic.List<TaskInfo> tasks = TaskInfoBussiness.GetList(NodeType.TimeTrigger, WorkflowID);
                            foreach (var task in tasks)
                            {
                                TaskInfoBussiness.Delete(task.TaskID);
                            }

                        }
                    }

                }
            }
            AppInfoBussiness.Update(App);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(string mid = "")
        {
            var App = mid == "" ? new AppInfo() : AppInfoBussiness.GetModel(mid);
            var ModelList = LargeModelInfoBussiness.GetList(" SystemStatus = 0 ");

            if (!mid.IsNullOrEmpty()) {
                App.SecretKey = ApisettingsInfoBussiness.GetModelByAppID(mid)?.SecretKey;
            }

            ViewBag.AppTypeList = BaseDictionaryInfoBussiness.GetChildList("应用类型");
            ViewBag.SessionModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Chat);
            ViewBag.VectorModelList = ModelList.FindAll(x => x.TypeCode == AIModelType.Embedding);

            ViewBag.App = App;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            ViewBag.KnowledgeBases = KnowledgeBaseInfoBussiness.GetList(" SystemStatus = 0 ");
            return View();
        }
        [HttpPost]
        public JsonMsg<string> AppSave(AppInfo App)
        {
            App.SecretKey = App.SecretKey.IsNullOrEmpty()? hashEncrypt.MD5System(Guid.NewGuid().ToString()): App.SecretKey;

            if (App.AppID.IsNullOrEmpty())
            { 
                App.AppID = Guid.NewGuid().ToString();
                
                App.CreateTime = DateTime.Now;
                AppInfoBussiness.Add(App);
            }
            else
            {

                AppInfoBussiness.Update(App);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> AppDel(string mid)
        {
            AppInfoBussiness.DeleteList(mid);

            ApisettingsInfoBussiness.DeleteListByAppID(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
