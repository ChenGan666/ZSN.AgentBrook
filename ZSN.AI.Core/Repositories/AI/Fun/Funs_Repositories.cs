
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Repositories.Base;

namespace ZSN.AI.Core.Repositories
{
    [ServiceDescription(typeof(IFuns_Repositories), ServiceLifetime.Scoped)]
    public class Funs_Repositories : Repository<Funs>, IFuns_Repositories
    {
    }
}
