using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ZSN.XinLing.Web.Manage.Controllers
{
    public class IndexController : Controller
    {


        public IActionResult Index()
        {
            return View();
        }

    }
}
