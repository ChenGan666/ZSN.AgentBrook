using ZSN.AI.Entity.Model.Enum;
using System.ComponentModel.DataAnnotations;

namespace ZSN.AI.Core.Repositories
{
    public partial class Funs
    {
        public string Id { get; set; }

        /// <summary>
        /// 接口描述
        /// </summary>
        [Required]
        public string Path { get; set; }
      
    }
}
