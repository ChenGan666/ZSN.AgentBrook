using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ZSN.AI.Entity.Object
{
    public partial class UploadInfo
    {
        public UploadFileItem File { get; set; }

        public List<UploadFileItem> FileList { get; set; }
    }
}
