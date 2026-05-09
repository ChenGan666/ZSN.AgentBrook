using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Functions;
using ZSN.AI.Service.WebHelpers;
using ZSN.AI.Core.Interface;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;
using ZSN.AI.Core.Utils;

namespace ZSN.AgentBrook.AutoJob
{
    public class MarkdownJob : JobBase, IJob
    {
        private readonly IChatService _chatService;
        private readonly IConfiguration _configuration;
        private readonly ILogger<FileConverts> _logger;
        
        public MarkdownJob(IChatService chatService, IConfiguration configuration, ILogger<FileConverts> logger) 
        { 
            _chatService = chatService;
            _configuration = configuration;
            _logger = logger;
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
                //获取需要AI执行的任务
                List<NodeType> nodeTypes = new List<NodeType>() { NodeType.NotNode_Markdown };
                List<TaskInfo> tasks = TaskInfoBussiness.GetList(0, nodeTypes, DateTime.Now, 1, 100);

                if (tasks != null && tasks.Count > 0)
                {
                    foreach (var task in tasks)
                    {
                        if (task != null)
                        {
                            num++;
                            await this.MarkdownWorkerAsync(task);
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
        private async Task MarkdownWorkerAsync(TaskInfo task)
        {
            TaskConfig taskConfig = task.TaskConfig;
            try
            {
                if (taskConfig.NotNodeConfig != null)
                {
                    MarkdownConfig fileChunkConfig = JsonConvert.DeserializeObject<MarkdownConfig>(JsonConvert.SerializeObject(taskConfig.NotNodeConfig));
                    List<ConvertToMarkdownFiles> result = new List<ConvertToMarkdownFiles>();
                    if (fileChunkConfig != null)
                    {
                        FileConverts _fileConverts = new FileConverts(_logger, _configuration, _chatService);

                        bool returnMarkdown = fileChunkConfig.reCallDataType == ReCallDataType.Markdown;

                        for (int i = 0; i < fileChunkConfig.sourceFile.Count; i++)
                        {
                            // 调用ToMarkdownFilesAsync方法，将文件转换为Markdown格式
                            ConvertToMarkdownFiles _result = await _fileConverts.ToMarkdownFilesAsync(
                                fileChunkConfig.sourceFile[i].FilePath,
                                fileChunkConfig.sourceFile[i].FileName,
                                fileChunkConfig.sourceFile[i].FileCode,
                                returnMarkdown,
                                fileChunkConfig.prompt);

                            result.Add(_result);
                        }

                        // 将结果保存到任务结果中
                        task.Results = new Results() { Data = result };
                        task.State = TaskState.Completed;
                    }
                    else
                    {
                        task.Results = new Results() { Data = "文件转换配置错误,MarkdownConfig解析失败" };
                        task.State = TaskState.Failure;
                    }
                    task.UpdateTime = DateTime.Now;
                    TaskInfoBussiness.Update(task);

                    // 如果配置了回调URL，则将结果发送到回调URL
                    if (!string.IsNullOrEmpty(fileChunkConfig.reCallUrl))
                    {
                        try
                        {
                            _logger.LogInformation($"正在发送结果到回调URL: {fileChunkConfig.reCallUrl}\n{JsonConvert.SerializeObject(result)}");
                            var (statusCode, responseContent) = await ZSN.Utils.Core.Helpers.HttpRequestHelper.HttpPostAsync(
                                fileChunkConfig.reCallUrl,
                                result,
                                Encoding.UTF8);

                            _logger.LogInformation($"回调URL返回状态码: {statusCode}, 响应内容: {responseContent}");

                            if (statusCode != HttpStatusCode.OK)
                            {
                                _logger.LogWarning($"回调URL返回非成功状态码: {statusCode}");
                            }
                        }
                        catch (Exception callbackEx)
                        {
                            _logger.LogError($"发送结果到回调URL时出错: {callbackEx.Message}");
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                task.Results = new Results() { Data = ex };
                task.State = TaskState.Failure;
                DefaultLogService.AddOperationLog(ErrorId, ex.Message);
                task.UpdateTime = DateTime.Now;
                TaskInfoBussiness.Update(task);
            }
        }
    }
}
