
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.Service.Controllers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class MemberManageController: AdminBaseController
    {
        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = MemberInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.MemberList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> Status(string mid, bool status)
        {
            var member = MemberInfoBussiness.GetModel(mid);
            member.MState = status ? 0 : 1;

            MemberInfoBussiness.Update(member);
            return JsonMsg<string>.OK("更新成功");
        }

        public JsonMsg<string> ResetPwd(string mid)
        {
            var member = MemberInfoBussiness.GetModel(mid);
            member.MPWD = hashEncrypt.MD5System(hashEncrypt.MD5System(ConfigHelper.GetString("DefaultPassword")));

            MemberInfoBussiness.Update(member);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(string mid = "")
        {
            var member = mid == "" ? new MemberInfo() : MemberInfoBussiness.GetModel(mid);
            var departmentList = DepartmentInfoBussiness.GetList(" dState = 0 ");
            var staffInfo = StaffInfoBussiness.GetModel(member.MemberID);

            ViewBag.Member = member;
            ViewBag.DepartmentList = departmentList;
            ViewBag.StaffInfo = staffInfo == null ? new StaffInfo() : staffInfo;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");
            ViewBag.DefaultPassword = ConfigHelper.GetString("DefaultPassword");
            return View();
        }

        [HttpPost]
        public JsonMsg<string> Save(string MemberID,string MPhoneNumber,string MNickName,string MIcon,DateTime MBirthday,string Department,int DepartmentID,string dName,string sCode,string sName,string sTitle,DateTime sEntryTime,string sEmail,string sPhone,int MState,string MPWD,int StaffID,int UserID)
        {
            MemberInfo member = new MemberInfo();
            member.MemberID = MemberID.SecureSQL();
            member.MPhoneNumber = MPhoneNumber.SecureSQL();
            member.MNickName = MNickName.SecureSQL();
            member.MIcon = MIcon.SecureSQL();
            member.MBirthday = MBirthday;
            member.MPWD = MPWD.SecureSQL();
            member.MState = MState;


            StaffInfo staffInfo = new StaffInfo();
            staffInfo.StaffID = StaffID;
            staffInfo.DepartmentID = DepartmentID;
            staffInfo.DName = dName.SecureSQL();
            staffInfo.SCode = sCode.SecureSQL();
            staffInfo.SName = sName.SecureSQL();
            staffInfo.STitle = sTitle.SecureSQL();
            staffInfo.SEntryTime = sEntryTime;
            staffInfo.SEmail = sEmail.SecureSQL();
            staffInfo.SPhone = sPhone.SecureSQL();

            if (member.MemberID.IsNullOrEmpty())
            {
                member.MemberID = hashEncrypt.MD5System(Guid.NewGuid().ToString());
                member.MAppendTime = DateTime.Now;
                member.MPWD = hashEncrypt.MD5System(hashEncrypt.MD5System(ConfigHelper.GetString("DefaultPassword")));
                MemberInfoBussiness.Add(member);

                //添加员工绑定
                staffInfo.MemberID = member.MemberID;
                StaffInfoBussiness.Add(staffInfo);

            }
            else
            {
                MemberInfo _member = MemberInfoBussiness.GetModel(member.MemberID);
                _member.MPhoneNumber = member.MPhoneNumber;
                _member.MNickName = member.MNickName;
                _member.MIcon = member.MIcon;
                _member.MBirthday = member.MBirthday;
                _member.MPWD = member.MPWD;
                _member.MState = member.MState;

                MemberInfoBussiness.Update(member);

                if (staffInfo.StaffID>0)
                {
                    StaffInfo _staff = StaffInfoBussiness.GetModel(staffInfo.StaffID);
                    _staff.DepartmentID = staffInfo.DepartmentID;
                    _staff.DName = staffInfo.DName;
                    _staff.SCode = staffInfo.SCode;
                    _staff.SName = staffInfo.SName;
                    _staff.STitle = staffInfo.STitle;
                    _staff.SEntryTime = staffInfo.SEntryTime;
                    _staff.SEmail = staffInfo.SEmail;

                    StaffInfoBussiness.Update(_staff);
                }
                else
                {
                    staffInfo.MemberID = member.MemberID;

                    StaffInfoBussiness.Add(staffInfo);
                }
            }
            
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> Del(string mid)
        {
            MemberInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
