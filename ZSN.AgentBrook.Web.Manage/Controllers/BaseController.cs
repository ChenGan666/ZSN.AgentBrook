
using ZSN.AI.Entity.Chat;
using ZSN.AI.Service.Controllers;

namespace ZSN.AgentBrook.Web.Manage.Controllers
{
    public class BaseController: CommonBaseController
    {
        public BaseController() {


        }
        protected void SetTitle(string type, string title)
        {
            ViewBag.Type = type;
            ViewBag.Title = title;
        }
    }
}
