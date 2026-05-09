using Microsoft.AspNetCore.Mvc;
using ZSN.AI.Service.WebHelpers;
using ZSN.Utils.Core.Utils;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AI.Service.Controllers;
using ZSN.AgentBrook.Web.Manage.Attributes;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [Area("Manage")]
    public class AdminBaseController: CommonBaseController
    {
        public HashEncrypt hashEncrypt = new HashEncrypt();
        public int ErrorId = 2;
        public AdminBaseController()
        {
            
        }
    }
}
