using System;
using ZSN.AI.DAL;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private static IDocumentImageManage DocumentImageInstance;
        private static readonly object DocumentImageLockObj = new object();

        public static IDocumentImageManage GetDocumentImage(string connectionName)
        {
            if (DocumentImageInstance == null)
            {
                lock (DocumentImageLockObj)
                {
                    if (DocumentImageInstance == null)
                    {
                        GetDocumentImageProvider(connectionName);
                    }
                }
            }
            return DocumentImageInstance;
        }

        private static void GetDocumentImageProvider(string connectionName)
        {
            var db = DbConfig.GetDbInfo(connectionName);
            var type = Type.GetType(
                $"ZSN.AI.DAL." + db.DbType + ".DocumentImageManage, ZSN.AI.DAL." + db.DbType,
                false, true);
            var provider = (IDocumentImageManage)Activator.CreateInstance(type);
            provider.SetConnectionName(connectionName);
            DocumentImageInstance = provider;
        }
    }
}
