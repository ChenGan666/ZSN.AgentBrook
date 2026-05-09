namespace ZSN.AI.BLL
{
    public interface IOperationLogService
    {
        void AddOperationLog(int markId, string logDetail, string logRemarks = "", string uid = null);
    }
}
