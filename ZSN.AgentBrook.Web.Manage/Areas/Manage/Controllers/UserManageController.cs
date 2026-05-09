
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Service.WebHelpers;
using Senparc.NeuChar.App.AppStore;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class UserManageController : AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = UserInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.UserList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> UserStatus(int mid, bool status)
        {
            var User = UserInfoBussiness.GetModel(mid);
            User.UState = status ? 0 : 1;

            UserInfoBussiness.Update(User);
            return JsonMsg<string>.OK("更新成功。");
        }

        public IActionResult UserEdit(int mid = 0)
        {
            var User = mid == 0 ? new UserInfo() : UserInfoBussiness.GetModel(mid);
            ViewBag.User = User;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");
            ViewBag.Permissions = User.PermissionCode;
            ViewBag.UserPopedomJson = UserInfoBussiness.GetUserPopedomToJsonStr();
            return View();
        }

        public IActionResult Password() {
            return View();
        }

        [HttpPost]
        public JsonMsg<string> SavePassword(string oldPassword,string newPassword) {

            UserInfo _User = UserService.CurrentUser;

            if(string.Compare(_User.UPWD,UserInfoBussiness.GetUserEncryptionPassword(_User.UserID.ToString(), oldPassword.Trim()), false) != 0)
            {
                return JsonMsg<string>.Error("原密码错误。", ErrorCode.PasswordError);
            }
            else
            {
                _User.UPWD = UserInfoBussiness.GetUserEncryptionPassword(_User.UserID.ToString(), newPassword.Trim());

                if (UserInfoBussiness.UpdatePassword(_User))
                {
                    return JsonMsg<string>.OK("修改成功，请重新登录。");
                }
                else {
                    return JsonMsg<string>.Error("密码修改失败。", ErrorCode.ServerError);
                }
            }
        }
        [HttpPost]
        public JsonMsg<string> UserSave(UserInfo User)
        {
            string _pwd = hashEncrypt.MD5System("12345678");
            if (User.UserID<=0)
            {
                UserInfo _User = UserInfoBussiness.GetModel(User.UName);
                if (_User != null)
                {
                    return JsonMsg<string>.Error("账号已经存在。", ErrorCode.DataAlreadyExists);
                }
                else
                {
                    User.UAppendTime = DateTime.Now;
                    User.UPWD = _pwd;
                    User.UserID = UserInfoBussiness.Add(User);
                    if (User.UserID > 0)
                    {
                        User.UPWD = UserInfoBussiness.GetUserEncryptionPassword(User.UserID.ToString(), _pwd);

                        UserInfoBussiness.UpdatePassword(_User);
                    }
                    else
                    {
                        return JsonMsg<string>.Error("创建失败。", ErrorCode.ServerError);
                    }
                }
            }
            else
            {
                UserInfo _User = UserInfoBussiness.GetModel(User.UserID);

                User.UPWD = _User.UPWD;

                UserInfoBussiness.Update(User);
            }
            return JsonMsg<string>.OK("创建成功。");
        }

        public JsonMsg<string> UserDel(string mid)
        {
            UserInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功。");
        }


    }
}
