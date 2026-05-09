using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Core.Common.Excel
{
    public class KMSExcelModel
    {
        [ExeclProperty("Question", 0)]
        public string Question { get; set; }

        [ExeclProperty("Answer", 1)]
        public string Answer { get; set; }
    }
}
