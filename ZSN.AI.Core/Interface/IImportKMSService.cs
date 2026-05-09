using ZSN.AI.Entity.Model;

namespace ZSN.AI.Core.Interface
{
    public interface IImportKMSService
    {
        Task<ImportKMSTaskReq> ImportKMSTask(ImportKMSTaskReq req);
    }
}
