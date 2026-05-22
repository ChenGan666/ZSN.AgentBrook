using DocumentFormat.OpenXml.Drawing.Charts;
using DocumentFormat.OpenXml.Office2021.DocumentTasks;

using J2N.Text;
using JiebaNet.Segmenter.Common;
using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.Utils.Core.Extensions;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.WebHelpers;
using Task = System.Threading.Tasks.Task;
using ZSN.AI.Node;

namespace ZSN.AgentBrook.AutoJob
{
    public class TimeTrigger : JobBase, IJob
    {
        Task IJob.Execute(IJobExecutionContext context)
        {
            var res = Auto();
            return res;
        }
        public async Task<int> Auto()
        {
            //Console.WriteLine("ZSN.AI.AutoJob.Job![JobEvent_TimeTrigger]");
            int num = 0;
            try
            {
                //获取任务
                System.Collections.Generic.List<TaskInfo> tasks = TaskInfoBussiness.GetList(0, NodeType.TimeTrigger, DateTime.Now, 1, 100);
                if (tasks != null && tasks.Count > 0)
                {
                    List<string> TaskIDList = new List<string>();
                    foreach (var task in tasks)
                    {
                        if (task != null)
                        {
                            num++;

                            var IntervalValue = task.IntervalValue;
                            bool Doing = false;
                            DateTime currentDate = DateTime.Now.Date;
                            // 使用StartTime作为每日触发时间点
                            DateTime taskTime = new DateTime(currentDate.Year, currentDate.Month, currentDate.Day, task.StartTime.Hour, task.StartTime.Minute, task.StartTime.Second);
                            try
                            {
                                switch (task.LoopType)
                                {
                                    case LoopType.NOLoop:
                                        Doing = true;
                                        break;
                                    case LoopType.Second:
                                        if ((DateTime.Now - task.UpdateTime).TotalSeconds > IntervalValue.Value[0])
                                        {
                                            Doing = true;
                                        }
                                        break;
                                    case LoopType.Day:
                                        if ((DateTime.Now - task.UpdateTime).TotalDays >= IntervalValue.Value[0] && Math.Abs((DateTime.Now - taskTime).TotalSeconds) <= 60)
                                        {
                                            Doing = true;
                                        }
                                        break;
                                    case LoopType.Week:
                                        string dayOfWeek = ((int)DateTime.Now.DayOfWeek).ToString();
                                        if (string.Join(",", IntervalValue.Value).IndexOf(dayOfWeek) > -1 && Math.Abs((DateTime.Now - taskTime).TotalSeconds) <= 60)
                                        {
                                            Doing = true;
                                        }
                                        break;
                                    case LoopType.Month:
                                        string day = ((int)DateTime.Now.Day).ToString();
                                        if (string.Join(",", IntervalValue.Value).IndexOf(day) > -1 && Math.Abs((DateTime.Now - taskTime).TotalSeconds) <= 60)
                                        {
                                            Doing = true;
                                        }
                                        break;
                                }

                                if (Doing)
                                {
                                    await this.Doing(task);
                                    task.RedoCount++;
                                }

                                // 处理任务状态和执行次数
                                if (task.LoopType != LoopType.NOLoop)
                                {
                                    // 循环任务
                                    if (task.RepeatValue == 0)
                                    {
                                        // 无限次执行,保持Waiting状态
                                        task.State = TaskState.Waiting;
                                        TaskInfoBussiness.Update(task);
                                    }
                                    else if (task.RedoCount >= task.RepeatValue)
                                    {
                                        // 已达到执行次数上限,标记为完成
                                        TaskIDList.Add(task.TaskID);
                                    }
                                    else
                                    {
                                        // 未达到执行次数上限,继续等待
                                        task.State = TaskState.Waiting;
                                        TaskInfoBussiness.Update(task);
                                    }
                                }
                                else
                                {
                                    // 非循环任务,执行一次后完成
                                    task.State = TaskState.Completed;
                                    TaskInfoBussiness.Update(task);
                                }
                            }
                            catch (Exception e)
                            {
                                task.State = TaskState.Failure;
                                // 错误处理:循环任务保持Waiting状态以便重试,非循环任务标记为失败
                                if (task.LoopType != LoopType.NOLoop)
                                {
                                    task.State = TaskState.Waiting;
                                }
                                TaskInfoBussiness.Update(task);
                                DefaultLogService.AddOperationLog(ErrorId, e.Message);
                            }
                        }
                    }

                    if (TaskIDList.Count > 0)
                    {
                        TaskInfoBussiness.SetState(TaskIDList, TaskState.Completed);
                    }
                }
            }
            catch (Exception e)
            {
                num = -1;
                DefaultLogService.AddOperationLog(ErrorId, e.Message);
            }
            return await Task.FromResult(num);
        }

        private async Task Doing(TaskInfo task)
        {
            TaskConfig taskConfig = task.TaskConfig;
            try
            {
                if (taskConfig.NodeConfig != null)
                {
                    if (taskConfig.NodeConfig.data != null)
                    {
                        string AppID = taskConfig.NodeConfig.mainid;
                        string NodeID = taskConfig.NodeConfig.id;
                        string WorkFlowID = taskConfig.NodeConfig.workflowid;

                        TaskData taskData = taskConfig.Data;
                        taskData.TaskID = task.TaskID;

                        TimeTriggerData timeTriggerData  = JsonConvert.DeserializeObject<TimeTriggerData>(taskConfig.NodeConfig.data.ToString());
                        if (timeTriggerData != null && timeTriggerData.enable)
                        {
                            string prompt = timeTriggerData.prompt;
                            List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(NodeID);
                            if (edgeList != null && edgeList.Count>0)
                            {
                                List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(string.Join(",", edgeList.Select(x=>$"'{x.TargetNodeId}'")));
                                if (targetNodeList != null)
                                {
                                    foreach (WorkflowNodeInfo node in targetNodeList)
                                    {
                                        NodeConfig targetNode = new NodeConfig();
                                        targetNode.id = node.NodeID;
                                        targetNode.mainid = taskConfig.NodeConfig.mainid;
                                        targetNode.workflowid = node.WorkflowID;
                                        targetNode.type = node.NodeType;
                                        targetNode.data = node.Config;
                                        targetNode.name = node.NodeName;

                                        string SessionID = Guid.NewGuid().ToString();
                                        string ProcessesID = Guid.NewGuid().ToString();

                                        List<Output> outputs = new List<Output>();
                                        outputs.Add(new Output { varname = "prompt", value = prompt });
                                        outputs.Add(new Output { varname = "currentTime", value = DateTime.Now.ToDateTimeString() });

                                        TaskInfoBussiness.toTask(taskConfig.NodeConfig, outputs, targetNode, AppID, SessionID, ProcessesID);
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                task.Results = new Results();
                task.Results.Data = ex;
                task.State = TaskState.Failure;

                DefaultLogService.AddOperationLog(ErrorId, ex.Message);
            }
            task.UpdateTime = DateTime.Now;
            TaskInfoBussiness.Update(task);
        }
    }
}
