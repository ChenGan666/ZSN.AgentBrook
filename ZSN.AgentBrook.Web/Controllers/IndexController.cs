using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.FileSystemGlobbing.Internal;
using System.Diagnostics;
using System.Text;
using System.Text.RegularExpressions;

namespace ZSN.XinLing.Web.Controllers
{
    public class IndexController : Controller
    {

        public IActionResult Index()
        {
            //将ANT.Design发布后的dist文件夹下的所有文件覆盖wwwroot文件夹下
            string htmlContent = System.IO.File.ReadAllText(Directory.GetCurrentDirectory()+@"\wwwroot\index.html"); 

            Regex headRegex = new Regex(@"<head\b[^>]*>(.*?)</head>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match headMatch = headRegex.Match(htmlContent);
            string headContent = headMatch.Success ? headMatch.Groups[1].Value : "Head tag not found.";

            Regex bodyRegex = new Regex(@"<body\b[^>]*>(.*?)</body>", RegexOptions.IgnoreCase | RegexOptions.Singleline);
            Match bodyMatch = bodyRegex.Match(htmlContent);
            string bodyContent = bodyMatch.Success ? bodyMatch.Groups[1].Value : "Body tag not found.";


            ViewBag.head = headContent;
            ViewBag.body = bodyContent;
            return View();
        }
    }
}
