
using System.Linq.Expressions;
using SqlSugar;

namespace ZSN.AI.Core.Repositories.Base
{
    public class Repository<T> : SimpleClient<T> where T : class, new()
    {

        public Repository(ISqlSugarClient context = null) : base(context)//注意这里要有默认值等于null
        {

            if (context == null)
            {
            }

        }

    }
}
