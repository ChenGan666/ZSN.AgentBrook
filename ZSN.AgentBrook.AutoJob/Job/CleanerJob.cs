using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.WebHelpers;

namespace ZSN.AgentBrook.AutoJob
{
    public class CleanerJob : JobBase, IJob
    {
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
                num++;
                await this.Cleaner();
            }
            catch (Exception e)
            {
                num = -1;
                DefaultLogService.AddOperationLog(ErrorId, e.Message);
            }
            return await Task.FromResult(num);
        }
        private async Task Cleaner()
        {
            //删除30天的系统日志
            LogRecordBusiness.DeleteByWhere($" CreateTime<='{DateTime.Now.AddDays(-30)}'");

            //删除30天的Node执行记录
            WorkflowNodeExecutionRecordInfoBussiness.DeleteByWhere($" EndTime<='{DateTime.Now.AddDays(-30)}'");

            //删除30天的任务记录
            TaskInfoBussiness.DeleteByWhere($" LoopType=0 and UpdateTime<='{DateTime.Now.AddDays(-30)}'");
        }
    }
}
