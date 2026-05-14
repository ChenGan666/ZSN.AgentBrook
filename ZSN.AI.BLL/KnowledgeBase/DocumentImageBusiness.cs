using ZSN.AI.DAL;
using ZSN.AI.Entity.KnowledgeBase;

namespace ZSN.AI.BLL
{
    public partial class DocumentImageBusiness
    {
        private const string ConnectionName = "KnowledgeBaseDb";

        public static DocumentImageInfo GetByImageId(string imageId)
        {
            return DatabaseProvider.GetDocumentImage(ConnectionName).DocumentImage_GetByImageId(imageId);
        }
    }
}
