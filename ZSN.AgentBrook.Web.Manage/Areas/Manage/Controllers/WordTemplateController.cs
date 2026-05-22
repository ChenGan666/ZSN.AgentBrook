using ZSN.AI.BLL;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using Newtonsoft.Json;
using ZSN.AI.Service.Controllers;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class WordTemplateController: AdminBaseController
    {
        public IActionResult Index(int index = 1, int size = 10)
        {
            var lst = WordTemplateInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.WordTemplateList = lst;
            return View();
        }
        [HttpPost]
        public JsonMsg<string> Status(string mid, bool status)
        {
            var wordTemplate = WordTemplateInfoBussiness.GetModel(mid);
            wordTemplate.SystemStatus = status ? 0 : 1; // 0: Normal, 1: Disabled
            WordTemplateInfoBussiness.Update(wordTemplate);
            return JsonMsg<string>.OK("更新成功");
        }
        public IActionResult Edit(string mid)
        {
            var wordTemplate = mid.IsNullOrEmpty() ? new WordTemplateInfo() : WordTemplateInfoBussiness.GetModel(mid);

            if (mid.IsNullOrEmpty())
            {
                wordTemplate.CreateTime = DateTime.Now;
                wordTemplate.UpdateTime = DateTime.Now;
            }

            ViewBag.WordTemplate = wordTemplate;
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            return View();
        }

        /// <summary>
        /// 读取模板文件的标签
        /// </summary>
        /// <param name="fileCode"></param>
        /// <returns></returns>
        [HttpPost]
        public JsonMsg<string> GetTemplateLabelData(string fileCode)
        {
            try
            {
                var fileInfo = FilesInfoBussiness.GetModel(fileCode);
                if (fileInfo == null)
                {
                    return JsonMsg<string>.Error("文件不存在",ErrorCode.FileNotExist);
                }
                string filePath = Path.Combine(fileInfo.FFilePath, fileInfo.FName);
                if (!System.IO.File.Exists(filePath))
                {
                    return JsonMsg<string>.Error("文件不存在", ErrorCode.FileNotExist);
                }

                var bookmarks = new Dictionary<string,string>();
                using (WordprocessingDocument doc = WordprocessingDocument.Open(filePath, false))
                {
                    var body = doc.MainDocumentPart.Document.Body;
                    var bookmarkStarts = body.Descendants<BookmarkStart>();
                    foreach (var bookmarkStart in bookmarkStarts)
                    {
                        if (bookmarkStart.Name != "_GoBack")
                        {
                            bookmarks.Add(bookmarkStart.Name, "");
                        }
                    }
                }

                return JsonMsg<string>.OK(JsonConvert.SerializeObject(bookmarks));
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error("读取书签失败: " + ex.Message,ErrorCode.ServerError);
            }
        }

        [HttpPost]
        public JsonMsg<string> Save(WordTemplateInfo wordTemplate)
        {
            if (wordTemplate.WordTemplateID.IsNullOrEmpty())
            {
                wordTemplate.WordTemplateID = Guid.NewGuid().ToString();
                wordTemplate.CreateTime = DateTime.Now;
                wordTemplate.UpdateTime = DateTime.Now;

                


                WordTemplateInfoBussiness.Add(wordTemplate);
            }
            else
            {
                wordTemplate.UpdateTime = DateTime.Now;
                WordTemplateInfoBussiness.Update(wordTemplate);
            }

            return JsonMsg<string>.OK("保存成功");
        }
        public JsonMsg<string> Del(string mid)
        {
            WordTemplateInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }
    }
}
