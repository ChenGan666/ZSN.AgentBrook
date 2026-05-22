using Microsoft.AspNetCore.Mvc;
using ZSN.AI.Service.WebHelpers;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.AgentBrook.Web.Manage.Attributes;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes(CheckUrl =false,CheckPermissions = false)]
    public class IndexController : AdminBaseController
    {
        public IActionResult Index()
        {
            List<MenuInfo> MenuInfos = MenuInfoBussiness.GetList(" MState=0 ");

            //过滤当前用户无权限的菜单
            UserInfo user = UserService.CurrentUser;

            MenuInfos = UserInfoBussiness.ReSetMenuByUserPopedom(user.PermissionCode);

            HashSet<MenuInfo> hashSet = new HashSet<MenuInfo>(MenuInfos);

            List<MenuInfo> newMenu = new List<MenuInfo>(hashSet);

            ViewBag.Users = user;
            ViewBag.Menus = newMenu;
            return View();
        }
    }
}