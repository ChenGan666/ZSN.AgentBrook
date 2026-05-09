using System;
using System.Collections.Generic;
using System.Linq;

namespace ZSN.AI.DAL
{
    public partial class DatabaseProvider
    {
        private DatabaseProvider()
        { }

        static DatabaseProvider()
        {
            InitProvider();
        }


        private static void InitProvider()
        {

        }

        public static void ResetDbProvider()
        {

        }
    }
}
