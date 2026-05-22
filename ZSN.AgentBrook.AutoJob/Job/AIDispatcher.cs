using Microsoft.SemanticKernel.ChatCompletion;
using Newtonsoft.Json.Linq;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Extensions;
using Lucene.Net.Util.Fst;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Utils;
using ZSN.AI.BLL;
using Elastic.Clients.Elasticsearch.Mapping;
using Newtonsoft.Json;

namespace ZSN.AgentBrook.AutoJob
{
    public class AIDispatcher : JobBase, IJob
    {
        private readonly IChatService _chatService;
        public AIDispatcher(IChatService chatService)
        {
            _chatService = chatService;
        }
        Task IJob.Execute(IJobExecutionContext context)
        {
            var res = Auto();
            return res;
        }
        public async Task<int> Auto()
        {
            //Console.WriteLine("ZSN.AI.AutoJob.Job![JobEvent_AIDispatcher]");
            int num = 0;
            try
            {
                //获取需要AI执行的任务
                List<TaskInfo> tasks = TaskInfoBussiness.GetList(0, NodeType.Reporter, DateTime.Now, 1, 100);
                if (tasks != null && tasks.Count > 0)
                {
                    foreach (var task in tasks)
                    {
                        if (task != null)
                        {
                            num++;
                            await this.AIWorkerAsync_Reporter(task);

                            //Thread thread = new Thread(() => AIWorkerAsync_Reporter(task.TaskConfig));
                            //thread.Start();
                        }
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

        /// <summary>
        /// AI记录员，将会话内容摘要处理，自动判断话题切换
        /// </summary>
        /// <param name="task"></param>
        /// <returns></returns>
        private async Task AIWorkerAsync_Reporter(TaskInfo task)
        {
            TaskConfig taskConfig = task.TaskConfig;
            try
            {
                if (taskConfig.NodeConfig != null)
                {
                    if (taskConfig.NodeConfig.data != null)
                    {
                        TaskData taskData = taskConfig.Data;
                        taskData.TaskID = task.TaskID;

                        ReporterData reporter = JsonConvert.DeserializeObject<ReporterData>(taskConfig.NodeConfig.data.ToString());
                        if (reporter != null)
                        {
                            if (reporter.model?.LargeModelID > 0)
                            {
                                ChatHistory history = new ChatHistory();
                                LargeModelInfo largeModel = LargeModelInfoBussiness.GetModel(reporter.model.LargeModelID);

                                string AppID = taskData.AppID;
                                string SessionID = taskData.SessionID;

                                //设置系统提示词
                                if (!reporter.prompt.IsNullOrEmpty())
                                {
                                    history.AddSystemMessage(reporter.prompt);
                                }


                                //获取回话记录
                                List<AppChatLogInfo> appChatLogs = AppChatLogInfoBussiness.GetListBySessionID(AppID, SessionID);

                                history = await _chatService.GetChatHistory(appChatLogs, history);

                                List<string> chatLogIDs = appChatLogs.Select(x => x.ChatLogID).ToList();

                                history.AddUserMessage("开始按照提示词的要求对刚才的回话进行总结。");

                                LargeModelConfig modelConfig = new LargeModelConfig();
                                modelConfig.Id = largeModel.LargeModelID.ToString();
                                modelConfig.Model = largeModel;

                                //var chatResult = _chatService.SendChatAsync(modelConfig, history);
                                var chatResult = _chatService.HistorySummarize(modelConfig, history);
                                StringBuilder rawContent = new StringBuilder();
                                Chats info = null;
                                List<Chats> MessageList = [];
                                string _Outpus = "";
                                int ChatCount = 0;

                                await foreach (var content in chatResult)
                                {
                                    if (info == null)
                                    {
                                        rawContent.Append(content.ConvertToString());
                                        info = new Chats();
                                        info.Id = Guid.NewGuid().ToString();
                                        info.UserName = AuthorRole.Assistant.ToString();
                                        info.AppId = AppID;
                                        info.Context = content.ConvertToString();
                                        info.CreateTime = DateTime.Now;

                                        MessageList.Add(info);
                                    }
                                    else
                                    {
                                        rawContent.Append(content.ConvertToString());
                                    }
                                    info.Context = rawContent.ToString();
                                }
                                _Outpus = info.Context;

                                GptMsg _msg = new GptMsg();
                                _msg.role = AuthorRole.Assistant.ToString();
                                _msg.content = info.Context;

                                AppChatSummaryInfo chatSummaryInfo = new AppChatSummaryInfo();
                                chatSummaryInfo.AppID = AppID;
                                chatSummaryInfo.ChatSessionID = SessionID;
                                chatSummaryInfo.Content = JsonConvert.SerializeObject(_msg);
                                chatSummaryInfo.CreateTime = DateTime.Now;
                                chatSummaryInfo.ChatLogIDList = JsonConvert.SerializeObject(chatLogIDs);

                                AppChatSummaryInfoBussiness.Add(chatSummaryInfo);

                                task.Results = new Results();
                                task.Results.Data = (new ChatSummaryData()).SummaryID = chatSummaryInfo.SummaryID;
                                task.State = TaskState.Completed;

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
