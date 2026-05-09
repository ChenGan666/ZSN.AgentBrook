
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Repositories.Base;

namespace ZSN.AI.Core.Repositories
{
    [ServiceDescription(typeof(IAIModels_Repositories), ServiceLifetime.Scoped)]
    public class AIModels_Repositories : Repository<AIModels>, IAIModels_Repositories
    {
    }
}
