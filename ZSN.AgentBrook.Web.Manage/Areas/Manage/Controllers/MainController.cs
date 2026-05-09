using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes(CheckUrl = false, CheckPermissions = false)]
    public class MainController: AdminBaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
