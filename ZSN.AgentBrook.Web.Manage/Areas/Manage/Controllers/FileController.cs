using Microsoft.AspNetCore.Mvc;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.Utils.Core.Helpers;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Enum;
using System;
using System.Linq;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using System.IO;
using System.Security.Cryptography;
using System.IO.Compression;
using System.Text;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class FileController : AdminBaseController
    {
        [HttpPost]
        [Consumes("multipart/form-data")]
        public JsonMsg<string> UploadZip(IFormFile file)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return JsonMsg<string>.Error("未选择文件", ErrorCode.ParamsError);
                }
                var ext = Path.GetExtension(file.FileName)?.ToLowerInvariant();
                if (ext != ".zip")
                {
                    return JsonMsg<string>.Error("仅支持zip文件", ErrorCode.ParamsError);
                }

                var baseDir = ConfigHelper.GetString("Skill:Directory");
                if (string.IsNullOrWhiteSpace(baseDir))
                {
                    return JsonMsg<string>.Error("未配置Skill:Directory", ErrorCode.ServerError);
                }

                string md5;
                using (var stream = file.OpenReadStream())
                using (var md5Alg = MD5.Create())
                {
                    md5 = BitConverter.ToString(md5Alg.ComputeHash(stream)).Replace("-", "").ToLowerInvariant();
                }

                var targetFolder = Path.Combine(baseDir, md5);
                if (!Directory.Exists(targetFolder))
                {
                    Directory.CreateDirectory(targetFolder);
                }

                var zipPath = Path.Combine(targetFolder, md5 + ".zip");
                using (var fs = new FileStream(zipPath, FileMode.Create, FileAccess.Write))
                {
                    file.CopyTo(fs);
                }

                try
                {
                    ZipFile.ExtractToDirectory(zipPath, targetFolder, true);
                }
                catch (InvalidDataException)
                {
                    return JsonMsg<string>.Error("压缩包解析失败", ErrorCode.ParamsError);
                }
                catch (Exception ex)
                {
                    return JsonMsg<string>.Error("解压失败:" + ex.Message, ErrorCode.ServerError);
                }
                finally
                {
                    try { System.IO.File.Delete(zipPath); } catch { }
                }

                return JsonMsg<string>.OK(md5);
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error("上传失败:" + ex.Message, ErrorCode.ServerError);
            }
        }

        public IActionResult DirExplorer(string folder)
        {
            ViewBag.BaseDir = ConfigHelper.GetString("Skill:Directory");
            ViewBag.Folder = folder ?? string.Empty;
            return View("../Shared/DirExplorer");
        }

        [HttpGet]
        public IActionResult ListChildren(string folder)
        {
            try
            {
                var baseDir = ConfigHelper.GetString("Skill:Directory");
                if (string.IsNullOrWhiteSpace(baseDir))
                    return Json(JsonMsg<string>.Error("未配置Skill:Directory", ErrorCode.ServerError));

                if (!SafePathHelper.TrySafePath(baseDir, folder, out var absPath, out var err))
                    return Json(JsonMsg<string>.Error(err ?? "非法访问", ErrorCode.ParamsError));

                if (!Directory.Exists(absPath))
                    return Json(JsonMsg<string>.OK(Newtonsoft.Json.JsonConvert.SerializeObject(new object[0])));

                var children = new List<object>();
                foreach (var dir in Directory.GetDirectories(absPath))
                {
                    var di = new DirectoryInfo(dir);
                    var rel = Path.GetRelativePath(baseDir, dir).Replace("\\", "/");
                    children.Add(new
                    {
                        name = di.Name,
                        path = rel,
                        type = "dir",
                        hasChildren = di.EnumerateFileSystemInfos().Any()
                    });
                }
                foreach (var file in Directory.GetFiles(absPath))
                {
                    var fi = new System.IO.FileInfo(file);
                    var rel = Path.GetRelativePath(baseDir, file).Replace("\\", "/");
                    children.Add(new
                    {
                        name = fi.Name,
                        path = rel,
                        type = "file",
                        size = fi.Length,
                        mtime = fi.LastWriteTime.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }
                return Json(children);
            }
            catch (Exception ex)
            {
                return Json(JsonMsg<string>.Error("读取失败:" + ex.Message, ErrorCode.ServerError));
            }
        }

        [HttpGet]
        public IActionResult Download(string path)
        {
            try
            {
                var baseDir = ConfigHelper.GetString("Skill:Directory");
                if (!SafePathHelper.TrySafePath(baseDir, path, out var absPath, out var err))
                    return BadRequest(err ?? "非法路径");
                if (!System.IO.File.Exists(absPath))
                    return NotFound();

                var contentType = "application/octet-stream";
                return PhysicalFile(absPath, contentType, Path.GetFileName(absPath));
            }
            catch
            {
                return StatusCode(500);
            }
        }

        [HttpGet]
        public IActionResult ReadFile(string path)
        {
            try
            {
                var baseDir = ConfigHelper.GetString("Skill:Directory");
                if (!SafePathHelper.TrySafePath(baseDir, path, out var absPath, out var err))
                    return Json(JsonMsg<string>.Error(err ?? "非法访问", ErrorCode.ParamsError));
                if (!System.IO.File.Exists(absPath))
                    return Json(JsonMsg<string>.Error("文件不存在", ErrorCode.DataNotExists));
                if (!IsEditable(absPath))
                    return Json(JsonMsg<string>.Error("不支持编辑的文件类型", ErrorCode.ParamsError));

                var content = System.IO.File.ReadAllText(absPath, Encoding.UTF8);
                return Json(JsonMsg<string>.OK(content));
            }
            catch (Exception ex)
            {
                return Json(JsonMsg<string>.Error("读取失败:" + ex.Message, ErrorCode.ServerError));
            }
        }

        [HttpPost]
        public JsonMsg<string> SaveFile(string path, string content)
        {
            try
            {
                var baseDir = ConfigHelper.GetString("Skill:Directory");
                if (!SafePathHelper.TrySafePath(baseDir, path, out var absPath, out var err))
                    return JsonMsg<string>.Error(err ?? "非法访问", ErrorCode.ParamsError);
                if (!System.IO.File.Exists(absPath))
                    return JsonMsg<string>.Error("文件不存在", ErrorCode.DataNotExists);
                if (!IsEditable(absPath))
                    return JsonMsg<string>.Error("不支持编辑的文件类型", ErrorCode.ParamsError);

                System.IO.File.WriteAllText(absPath, content ?? string.Empty, Encoding.UTF8);
                return JsonMsg<string>.OK("保存成功");
            }
            catch (Exception ex)
            {
                return JsonMsg<string>.Error("保存失败:" + ex.Message, ErrorCode.ServerError);
            }
        }

        private static bool IsEditable(string absPath)
        {
            var ext = Path.GetExtension(absPath)?.ToLowerInvariant();
            if (string.IsNullOrEmpty(ext)) return false;
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
            {
                ".txt",".md",".js",".ts",".json",".yml",".yaml",".ini",".cfg",".xml",".html",".css",".cs",".csproj",".py",".sh",".bat",".ps1"
            };
            return allowed.Contains(ext);
        }
    }
}
