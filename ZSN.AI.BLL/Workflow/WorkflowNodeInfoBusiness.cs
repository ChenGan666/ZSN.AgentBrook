using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using ZSN.AI.Entity;
using ZSN.AI.DAL;
using JiebaNet.Segmenter;
using Senparc.CO2NET.Extensions;
using System.Threading.Tasks;
using Newtonsoft.Json.Serialization;
using Newtonsoft.Json;
using ZSN.AI.Entity.ClawAI;
using ZSN.Utils.Core.Helpers;
using StackExchange.Redis;
namespace ZSN.AI.BLL
{
    public partial class WorkflowNodeInfoBussiness
    {
	    #region 基础信息
        private const string ConnectionName = "WorkflowDb";
        #endregion
        #region tb_workflow_node_info

        public static void Save(WorkflowNodeInfo model) {
            if (GetModel(model.NodeID) != null)
            {
                Update(model);
            }
            else
            {
                Add(model);
            }
        }
		/// <summary>
        /// 增加一条数据
        /// </summary>
		public static string Add(WorkflowNodeInfo model)
		{
			return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_Add(model);
		}
		/// <summary>
        /// 更新一条数据
        /// </summary>
		public static bool Update(WorkflowNodeInfo model)
		{
			return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_Update(model);
		}
        /// <summary>
        /// 删除一条数据
        /// </summary>
		public static bool Delete(string nodeID)
		{
			return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_Delete(nodeID);
		}
        /// <summary>
        /// 批量删除数据
        /// </summary>
		public static bool DeleteList(string nodeIDlist)
		{
			return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_DeleteList(nodeIDlist);
		}
        public static bool DeleteAbsentList(string nodeIDlist, string WorkflowID)
        {
            return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_DeleteAbsentList(nodeIDlist, WorkflowID);
        }
        public static bool DeleteByWorkflowID(string nodeIDlist, string WorkflowID)
        {
            return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_DeleteByWorkflowID(WorkflowID);
        }
        /// <summary>
        /// 得到一个对象实体
        /// </summary>
		public static ZSN.AI.Entity.WorkflowNodeInfo GetModel(string nodeID)
		{
			return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_GetModel(nodeID);
		}
        public static ZSN.AI.Entity.WorkflowNodeInfo GetAppMainAINode(string AppID)
        {
            WorkflowInfo appWorkflow = WorkflowInfoBussiness.GetModelByAppID(AppID);
            if (appWorkflow != null)
            {
                string strWhere = " NodeType = '" + NodeType.MainAI.ToString() + "' and WorkflowID='" + appWorkflow.WorkflowID + "'";
                List<WorkflowNodeInfo> workflowNodes = GetList(1, strWhere, "");
                if (workflowNodes != null)
                {
                    return workflowNodes[0];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static ZSN.AI.Entity.WorkflowNodeInfo GetAppStartNode(string AppID)
        {
            WorkflowInfo appWorkflow = WorkflowInfoBussiness.GetModelByAppID(AppID);
            if (appWorkflow != null)
            {
                string strWhere = " NodeType = '" + NodeType.Start.ToString() + "' and WorkflowID='" + appWorkflow.WorkflowID + "'";
                List<WorkflowNodeInfo> workflowNodes = GetList(1, strWhere, "");
                if (workflowNodes != null)
                {
                    return workflowNodes[0];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static ZSN.AI.Entity.WorkflowNodeInfo GetAgentStartNode(string AgentID)
        {
            WorkflowInfo appWorkflow = WorkflowInfoBussiness.GetModelByAgentID(AgentID);
            if (appWorkflow != null)
            {
                string strWhere = " NodeType = '" + NodeType.AgentStart.ToString() + "' and WorkflowID='" + appWorkflow.WorkflowID + "'";
                List<WorkflowNodeInfo> workflowNodes = GetList(1, strWhere, "");
                if (workflowNodes != null && workflowNodes.Count>0)
                {
                    return workflowNodes[0];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }
        public static ZSN.AI.Entity.WorkflowNodeInfo GetWorkFlowStartNode(string WorkFlowID)
        {
            return GetWorkFlowNode(WorkFlowID, NodeType.Start);
        }
        public static ZSN.AI.Entity.WorkflowNodeInfo GetWorkFlowAgentStartNode(string WorkFlowID)
        {
            return GetWorkFlowNode(WorkFlowID, NodeType.AgentStart);
        }
        public static ZSN.AI.Entity.WorkflowNodeInfo GetWorkFlowNode(string WorkFlowID, NodeType nodeType)
        {

            string strWhere = " NodeType = '" + nodeType.ToString() + "' and WorkflowID='" + WorkFlowID + "'";
            List<WorkflowNodeInfo> workflowNodes = GetList(1, strWhere, "");
            if (workflowNodes?.Count>0)
            {
                return workflowNodes[0];
            }
            else
            {
                return null;
            }

        }
        public static ZSN.AI.Entity.WorkflowNodeInfo GetAppReporterNode(string AppID)
        {
            WorkflowInfo appWorkflow = WorkflowInfoBussiness.GetModelByAppID(AppID);
            if (appWorkflow != null)
            {
                string strWhere = " NodeType = '" + NodeType.Reporter.ToString() + "' and WorkflowID='" + appWorkflow.WorkflowID + "'";
                List<WorkflowNodeInfo> workflowNodes = GetList(1, strWhere, "");
                if (workflowNodes != null)
                {
                    return workflowNodes[0];
                }
                else
                {
                    return null;
                }
            }
            else
            {
                return null;
            }
        }

        public static List<WorkflowNodeInfo> GetAllNextNodeListByNodeID(string NodeID)
        {
            return WorkflowNodeInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).GetAllNextNodeListByNodeID(NodeID).Tables[0]);
        }

        public static  List<WorkflowNodeInfo> GetListByNodeType(NodeType nodeType)
        {
            string strWhere = " NodeType = '" + nodeType.ToString() + "'";
            return GetList(strWhere);
        }
        public static List<WorkflowNodeInfo> GetListByNodeType(NodeType nodeType,string WorkflowID)
        {
            string strWhere = $" WorkflowID='{WorkflowID}' and NodeType = '{nodeType.ToString()}'";
            return GetList(strWhere);
        }
        public static List<WorkflowNodeInfo> GetListByNodeID(string NodeID = "")
        {
            string strWhere = $" NodeID in({NodeID})";
            return WorkflowNodeInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得数据列表
        /// </summary>
        public static List<WorkflowNodeInfo> GetList(string strWhere = "")
        {
            return WorkflowNodeInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_GetList(strWhere).Tables[0]);
        }
        /// <summary>
        /// 获得前几行数据
        /// </summary>
		public static List<WorkflowNodeInfo> GetList(int top, string strWhere, string filedOrder)
        {
            return WorkflowNodeInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_GetList(top, strWhere, filedOrder).Tables[0]);
        }
        /// <summary>
        /// 获取记录总数
        /// </summary>
		public static int GetRecordCount(string strWhere = "")
        {
            return DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_GetRecordCount(strWhere);
        }
        /// <summary>
        /// 分页获取数据列表
        /// </summary>
		public static List<WorkflowNodeInfo> GetListByPage(string strWhere, string orderBy, int startIndex, int endIndex)
        {
            return WorkflowNodeInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_GetListByPage(strWhere, orderBy, startIndex, endIndex).Tables[0]);
        }
		/// <summary>
        /// 分页获取数据列表
        /// </summary>
        /// <param name="pageSize">每页大小</param>
        /// <param name="pageIndex">页标</param>
        /// <param name="strWhere">查询条件</param>
        /// <param name="pagetotal">总页数</param>
        /// <param name="total">总数</param>
        /// <param name="orderType">排序规则， 默认降序，1降序，0升序</param>
        /// <param name="showName">显示字段，默认全部</param>
        /// <param name="orderKey">排序key，默认主键</param>
        /// <returns></returns>
		public static List<WorkflowNodeInfo> GetListByPage(int pageSize, int pageIndex, string strWhere, out int pagetotal, out int total, int orderType = 1, string showName = "*", string orderKey = "NodeID")
		{
            return WorkflowNodeInfoDataSet_ToList(DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_GetListByPage(pageSize, pageIndex, strWhere, out pagetotal, out total, orderType, showName, orderKey));
        }
		private static List<WorkflowNodeInfo> WorkflowNodeInfoDataSet_ToList(DataTable dt)
		{
			var rows = dt.Rows;
            var list = new List<WorkflowNodeInfo>();
            foreach (DataRow r in rows)
            {
                list.Add(DatabaseProvider.GetWorkflowNodeInfo(ConnectionName).WorkflowNodeInfo_DataRowToModel(r));
            }
            return list;
		}
        #endregion

        /// <summary>
        /// 下一节点任务
        /// </summary>
        /// <param name="AppID"></param>
        /// <param name="SessionID"></param>
        /// <param name="SourceNode"></param>
        /// <param name="outputs"></param>
        /// <param name="Logs"></param>
        /// <param name="AgentID">为Agent工作流时启用</param>
        public static string NextNode(string AppID, string SessionID, string ProcessesID,string FromTaskID,string FromMainTaskID, string AgentNodeID, NodeConfig SourceNode, List<Inputs> inputs, List<Output> outputs, List<string> Logs)
        {
            
            if (!FromTaskID.IsNullOrEmpty())
            {
                //更新上节点状态
                TaskInfoBussiness.updateTask(FromTaskID, TaskState.Completed, new Results()
                {
                    Data = outputs
                });
            }
            
            string newTaskID = "";
            List<WorkflowEdgeInfo> edgeList = WorkflowEdgeInfoBussiness.GetListBySourceNodeId(SourceNode.id);
            if (edgeList != null && edgeList.Count > 0)
            {
                List<WorkflowNodeInfo> targetNodeList = WorkflowNodeInfoBussiness.GetListByNodeID(string.Join(",", edgeList.Select(x => $"'{x.TargetNodeId}'")));
                if (targetNodeList != null)
                {
                    foreach (WorkflowNodeInfo node in targetNodeList)
                    {
                        //if (!AgentNodeID.IsNullOrEmpty())
                        {
                            NodeConfig targetNode = new NodeConfig();
                            targetNode.id = node.NodeID;
                            targetNode.mainid = SourceNode.mainid;
                            targetNode.workflowid = node.WorkflowID;
                            targetNode.type = node.NodeType;
                            targetNode.data = node.Config != null
                                ? Newtonsoft.Json.Linq.JObject.Parse(node.Config.ToString())
                                : null;
                            targetNode.name = node.NodeType.ToString();// node.NodeName;
                            targetNode.fromNodeType = SourceNode.type;

                            newTaskID = TaskInfoBussiness.toTask(SourceNode, outputs, targetNode, AppID, SessionID, ProcessesID, FromTaskID, FromMainTaskID, AgentNodeID,node.WorkflowID);

                            if (!string.IsNullOrEmpty(FromMainTaskID))
                            {
                            }

                            Logs.Add($"{newTaskID}");
                        }
                    }
                }
            }
            return newTaskID;
        }

        /// <summary>
        /// Agent流程结束后的处理,回调AgentNode
        /// </summary>
        /// <param name="AppID"></param>
        /// <param name="SessionID"></param>
        /// <param name="ProcessesID"></param>
        /// <param name="FromTaskID"></param>
        /// <param name="FromMainTaskID"></param>
        /// <param name="AgentNodeID"></param>
        /// <param name="SourceNode"></param>
        /// <param name="outputs"></param>
        /// <param name="Logs"></param>
        /// <returns></returns>
        public static async Task<string> AgentEndToNextNode(string AppID, string SessionID, string ProcessesID, string FromTaskID, string FromMainTaskID, string AgentNodeID, NodeConfig SourceNode, List<Output> outputs, List<string> Logs)
        {

            
            string newTaskID = Guid.NewGuid().ToString();

            NodeConfig targetNode = new NodeConfig();
            targetNode.id = AgentNodeID;
            targetNode.mainid = SourceNode.mainid;
            targetNode.workflowid = SourceNode.workflowid;
            targetNode.type = SourceNode.type;
            targetNode.data = SourceNode.data != null
                ? Newtonsoft.Json.Linq.JObject.Parse(SourceNode.data.ToString())
                : null;
            targetNode.name = SourceNode.type.ToString();
            targetNode.fromNodeType = NodeType.AgentEnd;

            List<Inputs> inputs = outputs.Select(output => new Inputs
            {
                id = Guid.NewGuid().ToString(), // 如果需要生成唯一 id
                sourceId = output.sourceId,
                varname = output.varname, // 如果想保留原 varname，可以赋值，否则用默认值
                value = output.value,
                type = output.type,
                txt = output.txt,
                
            }).ToList();

            TaskInfo taskInfo = new TaskInfo();
            taskInfo.TaskID = newTaskID;
            taskInfo.TaskType = SourceNode.type;
            taskInfo.TaskConfig = new TaskConfig();
            taskInfo.TaskConfig.NodeConfig = targetNode;
            taskInfo.TaskConfig.Data = new TaskData() { AppID = AppID, SessionID = SessionID, TaskID = newTaskID, FromMainTaskID = FromMainTaskID, ProcessesID = ProcessesID, AgentNodeID = AgentNodeID, Inputs = inputs };
            taskInfo.LoopType = LoopType.NOLoop;
            taskInfo.RepeatValue = 1;
            taskInfo.RedoCount = 0;
            taskInfo.CreateTime = DateTime.Now;
            taskInfo.UpdateTime = DateTime.Now;
            taskInfo.FromTaskID = FromTaskID;
            taskInfo.FromMainTaskID = FromMainTaskID;

            TaskInfoBussiness.Add(taskInfo);


            //更新上节点状态
            TaskInfoBussiness.updateTask(FromTaskID, TaskState.Completed, new Results()
            {
                Data = outputs
            });

            Logs.Add($"{newTaskID}");

            // ===== 检查是否有 ClawAI 步骤在等待此 WorkFlow 完成 =====
            await TryResumeClawAIStepAsync(FromMainTaskID, outputs, Logs);
            Logs.Add($"[ClawAI-Resume] AgentEndToNextNode TryResumeClawAIStepAsync 完成 - FromMainTaskID: {FromMainTaskID}");

            return newTaskID;
        }

        /// <summary>
        /// 检查并恢复等待中的 ClawAI 步骤
        /// 串行步骤：直接恢复
        /// 并行步骤：Redis 原子 DECR，最后一个完成者负责恢复
        /// </summary>
        public static async Task TryResumeClawAIStepAsync(
            string fromMainTaskID, List<Output> outputs, List<string> Logs)
        {
            try
            {
                if (string.IsNullOrEmpty(fromMainTaskID))
                {
                    return;
                }

                // 1. 查找关联的 ClawAI 异步等待 TaskInfo
                var asyncTaskInfo = TaskInfoBussiness.GetModel(fromMainTaskID);
                if (asyncTaskInfo == null)
                {
                    return;
                }
                if (asyncTaskInfo.TaskType != NodeType.ClawAIWorkflowStep)
                {
                    return;
                }
                // 状态检查：Waiting 或 Processing 都可恢复（GetList 可能将状态改为 Processing）
                if (asyncTaskInfo.State != TaskState.Waiting && asyncTaskInfo.State != TaskState.Processing)
                {
                    return;
                }

                // 2. 恢复上下文，获取层级信息
                var context = JsonConvert.DeserializeObject<ClawAIStepContext>(
                    JsonConvert.SerializeObject(asyncTaskInfo.TaskConfig.NotNodeConfig));
                if (context == null)
                {
                    return;
                }


                Logs.Add($"[ClawAI-Resume] 检测到 ClawAI 异步步骤等待中 - StepID: {context.TriggeredStepId}, " +
                         $"FromMainTaskID: {fromMainTaskID}");

                // 3. 提取并保存当前步骤结果到 Redis
                string workflowResult = outputs.FirstOrDefault(o => o.varname == "results")?.value ?? "";
                var redis = new RedisHelper().GetConnectionRedisMultiplexer().GetDatabase();
                string resultKey = $"clawai:result:{context.ProcessesID}:{context.TriggeredStepId}";
                redis.StringSet(resultKey, workflowResult, TimeSpan.FromHours(2));

                Logs.Add($"[并发控制] 步骤 {context.TriggeredStepIndex} 结果已保存 " +
                         $"(长度: {workflowResult?.Length ?? 0})");

                // 注意：不在恢复前将 TaskInfo 标记为 Completed，避免恢复失败后无法重试
                // 状态将在 ContinueFromStepAsync 成功完成后再更新

                // ✅ 增强：详细日志
                Logs.Add($"[ClawAI-Resume] ProcessesID: {context.ProcessesID}");
                Logs.Add($"[ClawAI-Resume] CurrentLayerIndex: {context.CurrentLayerIndex}");
                Logs.Add($"[ClawAI-Resume] TriggeredStepId: {context.TriggeredStepId}");

                // 4. 检查是否存在层级计数器（区分串行/并行）
                string layerCounterKey = $"clawai:layer:{context.ProcessesID}:{context.CurrentLayerIndex}";
                
                // ✅ 增强：记录检查
                Logs.Add($"[ClawAI-Resume] 检查计数器 Key: {layerCounterKey}");
                
                var counterExists = redis.KeyExists(layerCounterKey);
                
                // ✅ 增强：记录结果
                Logs.Add($"[ClawAI-Resume] 计数器存在: {counterExists}");

                if (!counterExists)
                {
                    // ===== 串行步骤（无计数器） → 直接恢复 =====
                    Logs.Add($"[ClawAI-Resume] 串行步骤完成，直接恢复: {fromMainTaskID}");

                    await FireAndResumeClawAI(fromMainTaskID, new Dictionary<string, string>
                    {
                        { context.TriggeredStepId, workflowResult }
                    }, Logs);
                    return;
                }

                // ===== 并行步骤 → 原子 DECR =====
                // ✅ 增强：记录 DECR 前的值
                var beforeValue = redis.StringGet(layerCounterKey);
                Logs.Add($"[并发控制] DECR 前计数器值: {beforeValue}");
                
                long remaining = redis.StringDecrement(layerCounterKey);

                Logs.Add($"[并发控制] DECR {layerCounterKey}: remaining={remaining}");

                if (remaining > 0)
                {
                    // 不是最后一个 → 只记录结果，不触发恢复
                    Logs.Add($"[并发控制] 本层还有 {remaining} 个步骤未完成，等待中");
                    return;
                }

                // ===== remaining == 0 → 本层全部完成，触发汇聚恢复 =====
                Logs.Add($"[并发控制] 本层全部完成，开始汇聚恢复");

                // 获取分布式锁（防止极端情况重复恢复）
                string lockKey = $"clawai:lock:{context.ProcessesID}";
                string lockValue = Guid.NewGuid().ToString();
                var distributedLock = new DistributedLock();

                // ✅ 增强：记录锁操作
                Logs.Add($"[并发控制] 尝试获取分布式锁: {lockKey}");

                if (!distributedLock.TryAcquire(lockKey, lockValue, TimeSpan.FromMinutes(5)))
                {
                    Logs.Add($"[并发控制] ❌ 获取恢复锁失败，可能已有其他线程在恢复");
                    return;
                }
                
                // ✅ 增强：记录锁成功
                Logs.Add($"[并发控制] ✅ 获取分布式锁成功");

                try
                {
                    // 从 Redis 读取层级上下文
                    string layerCtxKey = $"clawai:ctx:{context.ProcessesID}:{context.CurrentLayerIndex}";
                    
                    // ✅ 增强：记录读取
                    Logs.Add($"[并发控制] 读取层级上下文: {layerCtxKey}");
                    
                    var layerCtxJson = redis.StringGet(layerCtxKey);
                    if (layerCtxJson.IsNullOrEmpty)
                    {
                        Logs.Add($"[并发控制] ❌ 层级上下文已过期或不存在，跳过");
                        distributedLock.Release(lockKey, lockValue);
                        return;
                    }

                    // ✅ 增强：记录上下文内容
                    Logs.Add($"[并发控制] 上下文 JSON 长度: {layerCtxJson.ToString().Length}");

                    var layerCtx = JsonConvert.DeserializeObject<ClawAILayerContext>(
                        layerCtxJson.ToString());

                    // ✅ 增强：记录上下文详情
                    Logs.Add($"[并发控制] 上下文解析成功 - TotalStepCount: {layerCtx.TotalStepCount}, LayerIndex: {layerCtx.LayerIndex}");

                    // 收集所有并行步骤结果
                    var allResults = new Dictionary<string, string>();
                    foreach (var stepId in layerCtx.StepIds)
                    {
                        string stepResultKey = $"clawai:result:{context.ProcessesID}:{stepId}";
                        
                        // ✅ 增强：记录每个步骤的结果读取
                        var stepResult = redis.StringGet(stepResultKey);
                        string resultValue = stepResult.IsNullOrEmpty ? "" : stepResult.ToString();
                        allResults[stepId] = resultValue;
                        
                        Logs.Add($"[并发控制] 读取步骤结果 - StepID: {stepId}, 长度: {resultValue.Length}");
                    }

                    Logs.Add($"[并发控制] 汇聚完成: {allResults.Count}/{layerCtx.TotalStepCount} 个步骤");

                    // ✅ 直接执行恢复，不使用 Task.Run（确保完成后才释放锁）
                    try
                    {
                        
                        string mergedResult = string.Join("\n\n",
                            allResults.Values.Where(v => !string.IsNullOrEmpty(v)));

                        
                        // ✅ 直接 await，确保恢复完成
                        await ClawAIResumeCallback.ResumeAsync(
                            fromMainTaskID, mergedResult, allResults);
                        
                        Logs.Add($"[并发控制] 并行恢复成功完成");
                        
                        // ✅ 修复: 更新所有并行步骤的异步任务状态为 Completed
                        // 从 Redis 中查找每个步骤对应的 AsyncTaskID 并更新状态
                        foreach (var stepId in layerCtx.StepIds)
                        {
                            try
                            {
                                // 查询该步骤对应的 ClawAIWorkflowStep 任务
                                string strWhere = $" TaskType={((int)NodeType.ClawAIWorkflowStep)} " +
                                                  $"AND State IN (0, 1) " +  // Waiting 或 Processing
                                                  $"AND ProcessesID LIKE '%{stepId}%'";  // ProcessesID 包含 stepId
                                
                                var asyncTasks = TaskInfoBussiness.GetList(strWhere);
                                foreach (var asyncTask in asyncTasks)
                                {
                                    // 验证是否是当前层的任务
                                    if (asyncTask.ProcessesID.EndsWith($"_{stepId}"))
                                    {
                                        asyncTask.State = TaskState.Completed;
                                        asyncTask.UpdateTime = DateTime.Now;
                                        TaskInfoBussiness.Update(asyncTask);
                                    }
                                }
                            }
                            catch (Exception updateEx)
                            {
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        Logs.Add($"[ClawAI-Resume] 恢复执行失败: {ex.Message}");
                        var task = TaskInfoBussiness.GetModel(fromMainTaskID);
                        if (task != null)
                        {
                            task.State = TaskState.Failure;
                            task.Results = new Results { Data = ex.Message };
                            TaskInfoBussiness.Update(task);
                        }
                    }
                    finally
                    {
                        // 释放锁 + 清理 Redis 临时数据
                        distributedLock.Release(lockKey, lockValue);
                        foreach (var stepId in layerCtx.StepIds)
                        {
                            redis.KeyDelete($"clawai:result:{context.ProcessesID}:{stepId}");
                        }
                        redis.KeyDelete(layerCounterKey);
                        redis.KeyDelete($"clawai:ctx:{context.ProcessesID}:{context.CurrentLayerIndex}");
                    }
                }
                catch (Exception ex)
                {
                    // ❌ 移除这里的锁释放，避免重复释放
                    // 如果这里抛异常，说明还没进入 Task.Run，锁还没被传递
                    // 需要在这里释放锁
                    distributedLock.Release(lockKey, lockValue);
                    Logs.Add($"[并发控制] 汇聚异常（启动前失败）: {ex.Message}");
                }
            }
            catch (Exception ex)
            {
                Logs.Add($"[ClawAI-Resume] 检测回调失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 串行步骤：直接恢复（await，确保恢复完成）
        /// </summary>
        public static async Task FireAndResumeClawAI(
            string asyncTaskID,
            Dictionary<string, string> stepResults,
            List<string> Logs)
        {
            try
            {
                string merged = string.Join("\n\n",
                    stepResults.Values.Where(v => !string.IsNullOrEmpty(v)));
                await ClawAIResumeCallback.ResumeAsync(asyncTaskID, merged, stepResults);
            }
            catch (Exception ex)
            {
                Logs.Add($"[ClawAI-Resume] 串行恢复失败: {ex.Message}");
                var task = TaskInfoBussiness.GetModel(asyncTaskID);
                if (task != null)
                {
                    task.State = TaskState.Failure;
                    task.Results = new Results { Data = ex.Message };
                    TaskInfoBussiness.Update(task);
                }
            }
        }
    }
}
