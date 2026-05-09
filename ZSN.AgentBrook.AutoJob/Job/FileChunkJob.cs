using Newtonsoft.Json;
using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;
using ZSN.AI.Service.WebHelpers;

namespace ZSN.AgentBrook.AutoJob
{
    public class FileChunkJob : JobBase, IJob
    {
        private readonly IImportKMSService _importKMSService;
        public FileChunkJob(IImportKMSService importKMSService) {
            _importKMSService = importKMSService;
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
                List<NodeType> nodeTypes = new List<NodeType>() { NodeType.NotNode_FileChunk };
                List<TaskInfo> tasks = TaskInfoBussiness.GetList(0, nodeTypes, DateTime.Now, 1, 100);

                if (tasks != null && tasks.Count > 0)
                {
                    foreach (var task in tasks)
                    {
                        if (task != null)
                        {
                            num++;
                            await this.FileChunkWorkerAsync_Node(task);
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
        private async Task FileChunkWorkerAsync_Node(TaskInfo task)
        {
            TaskConfig taskConfig = task.TaskConfig;
            try
            {
                if (taskConfig.NotNodeConfig != null)
                {
                    FileChunkConfig fileChunkConfig = JsonConvert.DeserializeObject<FileChunkConfig>(JsonConvert.SerializeObject(taskConfig.NotNodeConfig));
                    
                    ImportKMSTaskReq importKMSTask = fileChunkConfig.ImportKMSTask;

                    await _importKMSService.ImportKMSTask(importKMSTask);

                    KnowledgeBaseFileInfoBussiness.Update(importKMSTask.KnowledgeBaseFile);

                    task.Results = new Results() { Data= importKMSTask.KnowledgeBaseFile };
                    task.State = TaskState.Completed;
                }
            }
            catch (Exception ex)
            {
                task.Results = new Results() { Data = ex};
                task.State = TaskState.Failure;
                DefaultLogService.AddOperationLog(ErrorId, ex.Message);
            }
            task.UpdateTime = DateTime.Now;
            TaskInfoBussiness.Update(task);
        }
    }
}
