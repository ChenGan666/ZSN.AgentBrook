using System.Data;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.DAL
{
    public partial interface IDocumentImageManage
    {
        string SetConnectionName(string connName);
        DocumentImageInfo DocumentImage_GetByImageId(string imageId);
    }
}
