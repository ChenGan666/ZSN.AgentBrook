
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Repositories.Base;

namespace ZSN.AI.Core.Repositories
{
    [ServiceDescription(typeof(IKmss_Repositories), ServiceLifetime.Scoped)]
    public class Kmss_Repositories : Repository<Kmss>, IKmss_Repositories
    {
    }
}
