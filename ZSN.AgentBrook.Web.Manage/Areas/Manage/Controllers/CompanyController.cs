using DocumentFormat.OpenXml.Bibliography;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Helpers;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class CompanyController: AdminBaseController
    {
        public IActionResult Edit()
        {
            CompanyInfo company = CompanyInfoBussiness.GetModel();

            ViewBag.Company = company != null ? company : new CompanyInfo();

            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");
            return View();
        }

        [HttpPost]
        public JsonMsg<CompanyInfo> Save(CompanyInfo company)
        {
            CompanyInfo _ci = CompanyInfoBussiness.GetModel();
            if (_ci != null)
            {
                _ci.CFullName = company.CFullName;
                _ci.CTitle = company.CTitle;
                _ci.CIDCode = company.CIDCode;
                _ci.CCity = company.CCity;
                _ci.CScale = company.CScale;
                _ci.CInfo = company.CInfo;
                _ci.CLogo = company.CLogo;
                _ci.AppID = company.AppID;
                _ci.SecretKey = company.SecretKey;
                if (CompanyInfoBussiness.Update(_ci))
                {
                    return JsonMsg<CompanyInfo>.OK(_ci);
                }
                else
                {
                    return JsonMsg<CompanyInfo>.Error(null, ErrorCode.ServerError);
                }
            }
            else
            {
                _ci = new CompanyInfo();
                _ci.CFullName = company.CFullName;
                _ci.CTitle = company.CTitle;
                _ci.CIDCode = company.CIDCode;
                _ci.CCity = company.CCity;
                _ci.CScale = company.CScale;
                _ci.CInfo = company.CInfo;
                _ci.CLogo = company.CLogo;
                _ci.AppID = company.AppID;
                _ci.SecretKey = company.SecretKey;

                _ci.CompanyID = CompanyInfoBussiness.Add(_ci);
                if (_ci.CompanyID > 0)
                {
                    return JsonMsg<CompanyInfo>.OK(_ci);
                }
                else
                {
                    return JsonMsg<CompanyInfo>.Error(null, ErrorCode.ServerError);
                }
            }
        }
    }
}
