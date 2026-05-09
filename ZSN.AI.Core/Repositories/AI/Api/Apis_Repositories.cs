
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Repositories.Base;

namespace ZSN.AI.Core.Repositories
{
    [ServiceDescription(typeof(IApis_Repositories), ServiceLifetime.Scoped)]
    public class Apis_Repositories : Repository<Apis>, IApis_Repositories
    {
    }
}
