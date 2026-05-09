
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Repositories.Base;
using ZSN.AI.Entity.Model.KmsDetail;

namespace ZSN.AI.Core.Repositories
{
    [ServiceDescription(typeof(IKmsDetails_Repositories), ServiceLifetime.Scoped)]
    public class KmsDetails_Repositories : Repository<KmsDetails>, IKmsDetails_Repositories
    {
    }
}
