
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Repositories.Base;

namespace ZSN.AI.Core.Repositories
{
    [ServiceDescription(typeof(IChats_Repositories), ServiceLifetime.Scoped)]
    public class Chats_Repositories : Repository<Chats>, IChats_Repositories
    {
    }
}
