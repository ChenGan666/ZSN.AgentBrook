using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Entity.Chat;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AI.Entity.Workflow
{
    public class WorkflowTester
    {
        public  WorkflowTester()
        { 
        }
        public string APIAppID { get; set; } = string.Empty;
        public string SecretKey { get; set; } = string.Empty;
        public string AppID { get;set; } = string.Empty;
        public string WorkflowID { get; set; } = string.Empty;
        public string MemberID { get; set; } = string.Empty;
        public string AccessToken { get; set; } = string.Empty;
        public string MemberToken { get; set; } = string.Empty;
        public string RefreshToken {get;set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }

        public string API { get; set; } = string.Empty;

        public static WorkflowTester Config
        {
            get
            {
                return ConfigHelper.GetSection("Tester").Get<WorkflowTester>();
            }
        }
    }
}
