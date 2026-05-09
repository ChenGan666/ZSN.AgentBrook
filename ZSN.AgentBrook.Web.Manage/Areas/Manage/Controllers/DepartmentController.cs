using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class DepartmentController: AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = DepartmentInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.DepartmentList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> Status(int mid, bool status)
        {
            var Department = DepartmentInfoBussiness.GetModel(mid);
            Department.DState = status ? 0 : 1;

            DepartmentInfoBussiness.Update(Department);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(int mid =0)
        {
            var Department = mid == 0 ? new DepartmentInfo() : DepartmentInfoBussiness.GetModel(mid);
            ViewBag.Department = Department;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");
            return View();
        }
        [HttpPost]
        public JsonMsg<string> Save(DepartmentInfo Department)
        {
            if (Department.DepartmentID<=0)
            {  
                Department.DAppendtime = DateTime.Now;
                DepartmentInfoBussiness.Add(Department);
            }
            else
            {
                DepartmentInfoBussiness.Update(Department);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> Del(string mid)
        {
            DepartmentInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
