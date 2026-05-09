namespace ZSN.AI.Service.WebHelpers
{
    public class OperationLogService : BLL.IOperationLogService
    {
        public void AddOperationLog(int markId, string logDetail, string logRemarks = "", string uid = null)
        {
            DefaultLogService.AddOperationLog(markId, logDetail, logRemarks, uid);
        }
    }
}
