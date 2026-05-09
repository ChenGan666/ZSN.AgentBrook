using DocumentFormat.OpenXml.Bibliography;
using Lucene.Net.Util.Fst;
using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel.ChatCompletion;
using MySqlX.XDevAPI;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity;
using ZSN.AI.Service.WebHelpers;

namespace ZSN.AgentBrook.AutoJob
{
    public class SessionTopicJob:JobBase, IJob
    {
        private readonly IChatService _chatService;
        public SessionTopicJob(IChatService chatService)
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
            int num = 0;
            try
            {
                //获取1分钟之前的会话
                List<AppChatSessionInfo> tasks = AppChatSessionInfoBussiness.GetList($" SystemStatus=0 and CreateTime>='{DateTime.Now.AddMinutes(-3).ToString("yyyy-MM-dd HH:mm:ss")}'");
                if (tasks != null && tasks.Count > 0)
                {
                    foreach (var task in tasks)
                    {
                        if (task != null)
                        {
                            num++;
                            Thread thread = new Thread(() => SessionTpoic(task));
                            thread.Start();
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
        /// 会话主题话题识别
        /// </summary>
        /// <param name="task"></param>
        private async void SessionTpoic(AppChatSessionInfo task) {

            try
            {
                ChatHistory history = new ChatHistory();
                LargeModelInfo largeModel = LargeModelInfoBussiness.GetDefaultModel();
                if (largeModel != null)
                {
                    List<AppChatLogInfo> appChatLogs = AppChatLogInfoBussiness.GetListBySessionID(task.AppID, task.ChatSessionID);
                    history.AddSystemMessage("你是一个高效的会话主题识别工程师,可以用非常简短精炼的语句（15个字内）提取表达对话的主题,因为你提取的主题将作为这个会话的标题。");
                    history = await _chatService.GetChatHistory(appChatLogs, history);

                    LargeModelConfig modelConfig = new LargeModelConfig();
                    modelConfig.Id = largeModel.LargeModelID.ToString();
                    modelConfig.Model = largeModel;

                    var chatResult = _chatService.HistorySummarize(modelConfig, history);
                    Chats info = new Chats();
                    StringBuilder rawContent = new StringBuilder();
                    string _Outpus = "";

                    await foreach (var content in chatResult)
                    {
                        rawContent.Append(content.ConvertToString());
                        info.Context = rawContent.ToString();
                    }

                    _Outpus = info.Context;

                    task.TopicSummary = _Outpus;
                    AppChatSessionInfoBussiness.Update(task);
                }
            }catch (Exception e)
            {
                DefaultLogService.AddOperationLog(ErrorId, e.Message);
            }
        }
    }
}
