using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Channels;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using StackExchange.Redis;
using ZSN.Utils.Core.Helpers;
using Newtonsoft.Json;

namespace ZSN.AI.Service.Helpers
{
    public record TaskStepMessage(ProcessInfo ProcessInfo, DateTime Timestamp);
    public class TaskManager
    {
        private readonly ConcurrentDictionary<string, Channel<TaskStepMessage>> _channels = new();

        // 通过 SessionID + ProcessesID + ChannelCode 组合键获取或创建通道
        public Channel<TaskStepMessage> GetChannel(string sessionID,string processesID,string channelCode)
        {
            return _channels.GetOrAdd(sessionID+processesID+ channelCode, _ => Channel.CreateUnbounded<TaskStepMessage>());
        }
        // 通过 SessionID + ChannelCode 组合键获取或创建通道(无 ProcessesID 场景)
        public Channel<TaskStepMessage> GetChannel(string sessionID, string channelCode)
        {
            return _channels.GetOrAdd(sessionID + channelCode, _ => Channel.CreateUnbounded<TaskStepMessage>());
        }
        public async System.Threading.Tasks.Task RunProcessAsync(string sessionID, TimeSpan timeOut, bool isAgentNode = false, string channelCode = "")
        {
            var channel = GetChannel(sessionID, channelCode);
            var writer = channel.Writer;
            // 委托到通用核心实现(无 processesID 情况)
            await RunCoreAsync(
                sessionID,
                "",
                timeOut,
                isAgentNode,
                writer,
                () => TaskInfoBussiness.GetListBySessionID(sessionID),
                () => WorkflowNodeExecutionRecordInfoBussiness.GetListBySessionID(sessionID)
            );
        }
        public async System.Threading.Tasks.Task RunProcessAsync(string sessionID,string processesID, TimeSpan timeOut,bool isAgentNode = false,string channelCode = "")
        {
            var channel = GetChannel(sessionID,processesID, channelCode);
            var writer = channel.Writer;
            // 委托到通用核心实现(含 processesID 情况)
            await RunCoreAsync(
                sessionID,
                processesID,
                timeOut,
                isAgentNode,
                writer,
                () => TaskInfoBussiness.GetListBySessionIDProcessesID(sessionID, processesID),
                () => WorkflowNodeExecutionRecordInfoBussiness.GetListBySessionIDProcessesID(sessionID, processesID)
            );
        }

        // 裁剪记录中不必要的负载,减小传输开销
        private static void PruneRecordPayloads(List<WorkflowNodeExecutionRecordInfo> records, bool pruneInputs = true)
        {
            if (!pruneInputs) return;
            foreach (var item in records)
            {
                item.Inputs = null;
            }
        }

        // 查找流程结束节点记录(普通流程使用 End,Agent 流程使用 AgentEnd)
        private static WorkflowNodeExecutionRecordInfo FindEndRecord(List<WorkflowNodeExecutionRecordInfo> records, bool isAgentNode)
        {
            return isAgentNode
                ? records.FirstOrDefault(e => e.NodeName != null && e.NodeName.StartsWith(NodeType.AgentEnd.ToString()))
                : records.FirstOrDefault(e => e.NodeName != null && e.NodeName.StartsWith(NodeType.End.ToString()));
        }

        // 计算流程状态:若存在结束记录,以其状态为准;否则根据任务集合状态推断
        private static ProcessStatus ComputeProcessStatus(List<TaskInfo> taskInfos, WorkflowNodeExecutionRecordInfo endRecord)
        {
            if (endRecord != null)
            {
                switch (endRecord.Status)
                {
                    case ExecutionRecordStatus.Success:
                        return ProcessStatus.Success;
                    case ExecutionRecordStatus.Fail:
                        return ProcessStatus.Fail;
                    case ExecutionRecordStatus.Running:
                        return ProcessStatus.Running;
                    case ExecutionRecordStatus.Timeout:
                        return ProcessStatus.Timeout;
                }
            }

            bool hasWaiting = taskInfos.Any(t => t.State == TaskState.Waiting);
            bool hasRunning = taskInfos.Any(t => t.State == TaskState.Processing);
            bool allSuccess = taskInfos.Count > 0 && taskInfos.All(t => t.TaskType == NodeType.End && t.State == TaskState.Completed);
            bool hasFailed = taskInfos.Any(t => t.State == TaskState.Failure);

            if (hasWaiting || hasRunning)
            {
                return ProcessStatus.Running;
            }
            else
            {
                if (allSuccess)
                {
                    return ProcessStatus.Success;
                }
                if (hasFailed)
                {
                    return ProcessStatus.Fail;
                }
            }
            return ProcessStatus.Running;
        }

        // 核心轮询与推送逻辑:统一节奏、超时控制、状态汇聚与消息写入
        private static async System.Threading.Tasks.Task RunCoreAsync(
            string sessionID,
            string processesID,
            TimeSpan timeOut,
            bool isAgentNode,
            ChannelWriter<TaskStepMessage> writer,
            Func<List<TaskInfo>> getTasks,
            Func<List<WorkflowNodeExecutionRecordInfo>> getRecords)
        {
            var startTime = DateTime.UtcNow; // 记录启动时间用于超时判断
            var processStatus = ProcessStatus.Running; // 初始为运行中
            string streamKey = null;
            IDatabase redisDb = null;
            string lastId = "0-0";

            if (!string.IsNullOrEmpty(processesID))
            {
                streamKey = StreamKey.Build(sessionID, processesID);
                redisDb = new RedisHelper().GetConnectionRedisMultiplexer().GetDatabase();
            }

            while (processStatus == ProcessStatus.Running)
            {
                // 超时则退出循环并标记为 Timeout
                if (DateTime.UtcNow - startTime > timeOut)
                {
                    processStatus = ProcessStatus.Timeout;
                    break;
                }

                var taskInfos = getTasks();
                if (taskInfos != null && taskInfos.Count > 0)
                {
                    var records = getRecords();
                    //Console.WriteLine(JsonConvert.SerializeObject(records));
                    if (records != null && records.Count > 0)
                    {
                        var endRecord = FindEndRecord(records, isAgentNode); // 尝试定位结束记录
                        processStatus = ComputeProcessStatus(taskInfos, endRecord); // 汇聚计算状态
                        PruneRecordPayloads(records, pruneInputs: processStatus == ProcessStatus.Running); // 流程结束后保留Inputs

                        var process = new ProcessInfo
                        {
                            SessionID = sessionID,
                            ProcessID = processesID,
                            Status = processStatus,
                            Results = endRecord != null ? endRecord.Outputs : string.Empty,
                            ExecutionRecordInfos = records
                        };

                        // 尝试读取本轮 Redis Stream 增量并附加到 ProcessInfo.StreamEnvelope
                        if (redisDb != null && streamKey != null)
                        {
                            var envelopes = new List<RedisStreamSync.StreamEnvelope>();
                            try
                            {
                                var entries = redisDb.StreamRead(streamKey, lastId);
                                foreach (var entry in entries)
                                {
                                    var bodyField = entry.Values.FirstOrDefault(v => v.Name == "body").Value;
                                    if (!bodyField.IsNullOrEmpty)
                                    {
                                        var json = (string)bodyField;
                                        var env = RedisStreamSync.StreamEnvelope.Deserialize(json);
                                        if (env != null) envelopes.Add(env);
                                    }
                                    lastId = entry.Id;
                                }
                            }
                            catch { }

                            if (envelopes.Count > 0)
                            {
                                process.StreamEnvelope = envelopes;
                            }
                        }

                        processStatus = process.Status; // 与计算结果保持一致
                        await writer.WriteAsync(new TaskStepMessage(process, DateTime.Now)); // 推送一次进度
                    }
                }

                await System.Threading.Tasks.Task.Delay(500); // 统一 500ms 轮询节奏
            }

            await System.Threading.Tasks.Task.Delay(500); // 收尾等待,避免客户端立刻断流
            writer.Complete(); // 关闭 SSE 通道写入端
        }
    }
}
