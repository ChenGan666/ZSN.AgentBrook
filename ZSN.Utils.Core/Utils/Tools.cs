using LitJson;
using Microsoft.CSharp;
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Hosting;
using ZSN.Utils.Core.DI;
using ZSN.Utils.Core.Extensions;
using ZSN.Utils.Core.Helpers;
using Microsoft.AspNetCore.Hosting;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Emit;

namespace ZSN.Utils.Core.Utils
{
    public class Tools
    {
        private static IHttpContextAccessor ContextAccessor => ServiceLocator.GetInstance<IHttpContextAccessor>();
        private static HttpContext HttpContext => ContextAccessor?.HttpContext;

        public static MemoryStream GetValidateImg(out string code, string bgImg = "/Images/vcodebg.png")
        {
            code = Tools.GetValidateCode();
            Random random = new Random();
            Bitmap bitmap = new Bitmap((int)Math.Ceiling((double)code.Length * 17.2), 28);
            string path1 = bgImg;
            string filename = PathHelper.Combine(HttpContextHelper.Environment.WebRootPath, path1);
            System.Drawing.Image image = System.Drawing.Image.FromFile(filename);
            Graphics graphics = Graphics.FromImage((System.Drawing.Image)bitmap);
            Font font1 = new Font("Arial", 16f, FontStyle.Italic);
            Font font2 = new Font("Arial", 16f, FontStyle.Italic);
            LinearGradientBrush linearGradientBrush = new LinearGradientBrush(new Rectangle(0, 0, bitmap.Width, bitmap.Height), Color.Blue, Color.DarkRed, 1.2f, true);
            graphics.DrawImage(image, 0, 0, new Rectangle(random.Next(image.Width - bitmap.Width), random.Next(image.Height - bitmap.Height), bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
            graphics.DrawString(code, font2, Brushes.White, 0.0f, 1f);
            graphics.DrawString(code, font1, Brushes.Green, 0.0f, 1f);
            int width = bitmap.Width;
            int y1 = random.Next(5, bitmap.Height);
            int y2 = random.Next(5, bitmap.Height);
            graphics.DrawLine(new Pen(Color.Green, 2f), 1, y1, width - 2, y2);
            graphics.DrawRectangle(new Pen(Color.Transparent), 0, 10, bitmap.Width - 1, bitmap.Height - 1);
            MemoryStream memoryStream = new MemoryStream();
            bitmap.Save((Stream)memoryStream, ImageFormat.Png);
            return memoryStream;
        }

        private static string GetValidateCode()
        {
            string empty = string.Empty;
            Random random = new Random(Guid.NewGuid().GetHashCode());
            for (int index = 0; index < 4; ++index)
            {
                int num = random.Next();
                char ch = num % 2 != 0 ? (num % 3 != 0 ? (char)(65U + (uint)(ushort)(num % 26)) : (char)(97U + (uint)(ushort)(num % 26))) : (char)(48U + (uint)(ushort)(num % 10));
                empty += ch == '0' || ch == 'O' ? "x" : ch.ToString();
            }
            return empty;
        }

        public static string GetIPAddress()
        {
            if (HttpContext == null || HttpContext.Request == null)
                return "";
            return HttpContext.GetClientUserIp();
        }

        public static string GetBrowse()
        {
            if (HttpContext == null || HttpContext.Request == null)
                return "";
            return HttpContext.Request.Browser().Name;
        }

        public static string GetOSName()
        {
            if (HttpContext == null || HttpContext.Request == null)
                return "";
            string str = "";
            string userAgent = HttpContext.Request.UserAgent();
            if (userAgent.Contains("NT 10"))
                str = "Windows10";
            else if (userAgent.Contains("NT 6.3"))
                str = "Windows8.1";
            else if (userAgent.Contains("NT 6.2"))
                str = "Windows8";
            else if (userAgent.Contains("NT 6.1"))
                str = "Windows7";
            else if (userAgent.Contains("NT 6.0"))
                str = "WindowsVista";
            else if (userAgent.Contains("NT 5.2"))
                str = "WindowsServer2003";
            else if (userAgent.Contains("NT 5.1"))
                str = "WindowsXP";
            else if (userAgent.Contains("NT 5"))
                str = "Windows2000";
            else if (userAgent.Contains("NT 4"))
                str = "WindowsNT4.0";
            else if (userAgent.Contains("Me"))
                str = "WindowsMe";
            else if (userAgent.Contains("98"))
                str = "Windows98";
            else if (userAgent.Contains("95"))
                str = "Windows95";
            else if (userAgent.Contains("Mac"))
                str = "Mac";
            else if (userAgent.Contains("Unix"))
                str = "UNIX";
            else if (userAgent.Contains("Linux"))
                str = "Linux";
            else if (userAgent.Contains("SunOS"))
                str = "SunOS";
            return str;
        }

        public static string GetPagerHtml(long recordCount, int pageSize, int pageNumber, string queryString)
        {
            if (pageSize <= 0)
                pageSize = Tools.GetPageSize();
            long num1 = recordCount <= 0L ? 1L : (recordCount % (long)pageSize == 0L ? recordCount / (long)pageSize : recordCount / (long)pageSize + 1L);
            long num2 = (long)pageNumber;
            if (num2 < 1L)
                num2 = 1L;
            else if (num2 > num1)
                num2 = num1;
            if (num1 <= 1L)
                return "";
            StringBuilder stringBuilder1 = new StringBuilder(1500);
            string empty = string.Empty;
            int num3 = 10;
            stringBuilder1.Append("<div>");
            stringBuilder1.Append("<span style='margin-right:15px;'>共 " + recordCount.ToString() + " 条  每页 <input type='text' id='tnt_count' title='输入数字可改变每页显示条数' class='pagertxt' onchange=\"javascript:_toPage_" + empty + "(" + num2.ToString() + ",this.value);\" value='" + pageSize.ToString() + "' /> 条  ");
            stringBuilder1.Append("转到 <input type='text' id='paernumbertext' title='输入数字可跳转页' value=\"" + num2.ToString() + "\" onchange=\"javascript:_toPage_" + empty + "(this.value," + pageSize.ToString() + ");\" class='pagertxt'/> 页</span>");
            long num4;
            if (num2 > 1L)
            {
                StringBuilder stringBuilder2 = stringBuilder1;
                string[] strArray = new string[7] { "<a class=\"pager\" href=\"javascript:_toPage_", empty, "(", null, null, null, null };
                int index = 3;
                num4 = num2 - 1L;
                string str1 = num4.ToString();
                strArray[index] = str1;
                strArray[4] = ",";
                strArray[5] = pageSize.ToString();
                strArray[6] = ");\"><span class=\"pagerarrow\">«</span></a>";
                string str2 = string.Concat(strArray);
                stringBuilder2.Append(str2);
            }
            if (num2 >= (long)(num3 / 2 + 3))
                stringBuilder1.Append("<a class=\"pager\" href=\"javascript:_toPage_" + empty + "(1," + pageSize.ToString() + ");\">1…</a>");
            else
                stringBuilder1.Append("<a class=\"" + (1L == num2 ? "pagercurrent" : "pager") + "\" href=\"javascript:_toPage_" + empty + "(1," + pageSize.ToString() + ");\">1</a>");
            long num5 = num2 - (long)(num3 / 2);
            long num6 = num2 + (long)(num3 / 2);
            if (num5 < 2L)
            {
                num6 += 2L - num5;
                num5 = 2L;
            }
            if (num6 > num1 - 1L)
            {
                num5 -= num6 - (num1 - 1L);
                num6 = num1 - 1L;
            }
            if (num5 < 2L)
                num5 = 2L;
            for (long index = num5; index <= num6; ++index)
                stringBuilder1.Append("<a class=\"" + (index == num2 ? "pagercurrent" : "pager") + "\" href=\"javascript:_toPage_" + empty + "(" + index.ToString() + "," + pageSize.ToString() + ");\">" + index.ToString() + "</a>");
            if (num2 <= num1 - (long)(num3 / 2))
                stringBuilder1.Append("<a class=\"pager\" href=\"javascript:_toPage_" + empty + "(" + num1.ToString() + "," + pageSize.ToString() + ");\">…" + num1.ToString() + "</a>");
            else if (num1 > 1L)
                stringBuilder1.Append("<a class=\"" + (num1 == num2 ? "pagercurrent" : "pager") + "\" href=\"javascript:_toPage_" + empty + "(" + num1.ToString() + "," + pageSize.ToString() + ");\">" + num1.ToString() + "</a>");
            if (num2 < num1)
            {
                StringBuilder stringBuilder2 = stringBuilder1;
                string[] strArray = new string[7] { "<a class=\"pager\" href=\"javascript:_toPage_", empty, "(", null, null, null, null };
                int index = 3;
                num4 = num2 + 1L;
                string str1 = num4.ToString();
                strArray[index] = str1;
                strArray[4] = ",";
                strArray[5] = pageSize.ToString();
                strArray[6] = ");\"><span class=\"pagerarrow\">»</span></a>";
                string str2 = string.Concat(strArray);
                stringBuilder2.Append(str2);
            }
            stringBuilder1.Append("</div>");
            stringBuilder1.Append("<script type=\"text/javascript\" lanuage=\"javascript\">");
            stringBuilder1.Append("function _toPage_" + empty + "(page,size){");
            stringBuilder1.Append("var par=\"" + queryString + "\";");
            stringBuilder1.Append("window.location=\"?pagenumber=\"+page+\"&pagesize=\"+size+par;");
            stringBuilder1.Append("}");
            stringBuilder1.Append("</script>");
            return stringBuilder1.ToString();
        }

        public static int GetPageSize()
        {
            string str = HttpContext.Request.GetParam("pagesize");
            if (!str.IsInt())
            {
                var cookie = HttpContext.Request.Cookies["roadflowpagesize"];
                if (cookie != null)
                    str = cookie;
            }
            int test;
            int num = str.IsInt(out test) ? test : 15;
            if (num <= 0)
                num = 15;
            HttpContext.Response.Cookies.Append("roadflowpagesize"
                ,num.ToString(),
                new CookieOptions
                {
                    Expires =DateTime.Now.AddYears(2)
                }
            );
            return num;
        }

        public static int GetPageNumber()
        {
            int test;
            if (!(HttpContext.Request.GetParam("pagenumber").ToString() ?? "1").IsInt(out test))
                return 1;
            return test;
        }

        public static ListItem[] GetListItems(IList<string[]> list, string value, bool showEmpty = false, string emptyTitle = "")
        {
            List<ListItem> listItemList = new List<ListItem>();
            if (showEmpty)
                listItemList.Add(new ListItem(emptyTitle, ""));
            foreach (string[] strArray in (IEnumerable<string[]>)list)
            {
                if (strArray.Length >= 2)
                    listItemList.Add(new ListItem(strArray[0], strArray[1])
                    {
                        Selected = !value.IsNullOrEmpty() && value == strArray[1] && !listItemList.Exists((Predicate<ListItem>)(p => p.Selected))
                    });
            }
            return listItemList.ToArray();
        }

        public static ListItem[] GetListItems(IList<string> list, string value, bool showEmpty = false, string emptyTitle = "")
        {
            List<string[]> strArrayList = new List<string[]>();
            foreach (string str in (IEnumerable<string>)list)
                strArrayList.Add(new string[2] { str, str });
            return Tools.GetListItems((IList<string[]>)strArrayList, value, showEmpty, emptyTitle);
        }

        public static string GetOptionsString(ListItem[] items)
        {
            StringBuilder stringBuilder = new StringBuilder(items.Length * 50);
            foreach (ListItem listItem in items)
            {
                stringBuilder.AppendFormat("<option value=\"{0}\" {1}>", (object)listItem.Value.Replace("\"", "'"), listItem.Selected ? (object)"selected=\"selected\"" : (object)"");
                stringBuilder.Append(listItem.Text);
                stringBuilder.Append("</option>");
            }
            return stringBuilder.ToString();
        }

        public static string GetCheckBoxString(ListItem[] items, string name, string[] values, string otherAttr = "")
        {
            StringBuilder stringBuilder = new StringBuilder(items.Length * 50);
            foreach (ListItem listItem in items)
            {
                string str = Guid.NewGuid().ToString("N");
                stringBuilder.AppendFormat("<input type=\"checkbox\" value=\"{0}\" {1} id=\"{2}\" name=\"{3}\" {4} style=\"vertical-align:middle\" />", (object)listItem.Value.Replace("\"", "'"), listItem.Selected || values != null && ((IEnumerable<string>)values).Contains<string>(listItem.Value) ? (object)"checked=\"checked\"" : (object)"", (object)string.Format("{0}_{1}", (object)name, (object)str), (object)name, (object)otherAttr);
                stringBuilder.AppendFormat("<label style=\"vertical-align:middle;margin-right:2px;\" for=\"{0}\">", (object)string.Format("{0}_{1}", (object)name, (object)str));
                stringBuilder.Append(listItem.Text);
                stringBuilder.Append("</label>");
            }
            return stringBuilder.ToString();
        }

        public static string GetRadioString(ListItem[] items, string name, string otherAttr = "")
        {
            StringBuilder stringBuilder = new StringBuilder(items.Length * 50);
            foreach (ListItem listItem in items)
            {
                string str = Guid.NewGuid().ToString("N");
                stringBuilder.AppendFormat("<input type=\"radio\" value=\"{0}\" {1} id=\"{2}\" name=\"{3}\" {4} style=\"vertical-align:middle\" />", (object)listItem.Value.Replace("\"", "'"), listItem.Selected ? (object)"checked=\"checked\"" : (object)"", (object)string.Format("{0}_{1}", (object)name, (object)str), (object)name, (object)otherAttr);
                stringBuilder.AppendFormat("<label style=\"vertical-align:middle;margin-right:2px;\" for=\"{0}\">", (object)string.Format("{0}_{1}", (object)name, (object)str));
                stringBuilder.Append(listItem.Text);
                stringBuilder.Append("</label>");
            }
            return stringBuilder.ToString();
        }

        public static ListItem[] GetYesNoListItems(string value, bool showEmpty = false, string emptyString = "")
        {
            return Tools.GetListItems((IList<string[]>)new List<string[]>() { new string[2] { "是", "1" }, new string[2] { "否", "0" } }, value, showEmpty, emptyString);
        }

        public static string GetSqlInString(string str, bool isSingleQuotes = true, string split = ",")
        {
            return Tools.GetSqlInString<string>(str.Split(new string[1] { split }, StringSplitOptions.RemoveEmptyEntries), isSingleQuotes);
        }

        public static string GetSqlInString<T>(T[] strArray, bool isSingleQuotes = true)
        {
            StringBuilder stringBuilder = new StringBuilder(strArray.Length * 40);
            foreach (T str in strArray)
            {
                if (!str.ToString().IsNullOrEmpty())
                {
                    if (isSingleQuotes)
                        stringBuilder.Append("'");
                    stringBuilder.Append(str.ToString().Trim());
                    if (isSingleQuotes)
                        stringBuilder.Append("'");
                    stringBuilder.Append(",");
                }
            }
            return stringBuilder.ToString().TrimEnd(',');
        }

        public static int[] GetRandomNum(int count, int minValue, int maxValue)
        {
            byte[] data = new byte[4];
            new RNGCryptoServiceProvider().GetBytes(data);
            Random random = new Random(BitConverter.ToInt32(data, 0));
            int length = maxValue - minValue + 1;
            byte[] keys = new byte[length];
            byte[] buffer = keys;
            random.NextBytes(buffer);
            int[] items = new int[length];
            for (int index = 0; index < length; ++index)
                items[index] = index + minValue;
            Array.Sort<byte, int>(keys, items);
            int[] numArray = new int[count];
            Array.Copy((Array)items, (Array)numArray, count);
            return numArray;
        }

        public static string GetRandomString(int length = 5)
        {
            string empty = string.Empty;
            byte[] data = new byte[4];
            new RNGCryptoServiceProvider().GetBytes(data);
            Random random = new Random(BitConverter.ToInt32(data, 0));
            for (int index = 0; index < length + 1; ++index)
            {
                int num = random.Next();
                char ch = num % 2 != 0 ? (char)(65U + (uint)(ushort)(num % 26)) : (char)(48U + (uint)(ushort)(num % 10));
                empty += ch.ToString();
            }
            return empty;
        }

        public static string GetRandomLetter(int length = 2)
        {
            string empty = string.Empty;
            byte[] data = new byte[4];
            new RNGCryptoServiceProvider().GetBytes(data);
            Random random = new Random(BitConverter.ToInt32(data, 0));
            for (int index = 0; index < length; ++index)
            {
                char ch = (char)(65U + (uint)(ushort)(random.Next() % 26));
                empty += ch.ToString();
            }
            return empty;
        }

        public static string GetFileSize(string file)
        {
            if (!File.Exists(file))
                return "";
            return (new FileInfo(file).Length / 1000L).ToString("###,###");
        }

        public static string DataTableToJsonString(DataTable dt)
        {
            JsonData jsonData1 = new JsonData();
            foreach (DataRow row in (InternalDataCollectionBase)dt.Rows)
            {
                JsonData jsonData2 = new JsonData();
                for (int index = 0; index < dt.Columns.Count; ++index)
                {
                    JsonData jsonData3 = new JsonData();
                    JsonData jsonData4 = (JsonData)row[index].ToString();
                    jsonData2.Add((object)jsonData4);
                }
                jsonData1.Add((object)jsonData2);
            }
            return jsonData1.ToJson();
        }

        public static object ExecuteCsharpMethod(string methodName, object methodParams = null)
        {
            try
            {
                //更改反射  例子:FJData.DaTaOAProject.Web.Common.HomePage.GetOnlineUsersCount
                string withoutExtension = Path.GetFileNameWithoutExtension(methodName);
                //   Assembly assembly = Assembly.Load(methodName.Substring(0, methodName.IndexOf('.')));
                string temp = Path.GetFileNameWithoutExtension(withoutExtension);
                Assembly assembly = Assembly.Load(Path.GetFileNameWithoutExtension(temp));
                string name1 = methodName.Substring(withoutExtension.Length + 1);
                string name2 = withoutExtension;
                int num = 1;
                Type type = assembly.GetType(name2, num != 0);
                object instance = Activator.CreateInstance(type, false);
                MethodInfo method = type.GetMethod(name1);
                if (!(method != (MethodInfo)null))
                    return (object)null;
                MethodInfo methodInfo = method;
                object obj = instance;
                object[] parameters;
                if (methodParams == null)
                    parameters = (object[])null;
                else
                    parameters = new object[1] { methodParams };
                return methodInfo.Invoke(obj, parameters);
            }
            catch
            {
                return (object)null;
            }
        }

        public static object ExecuteCsharpCode(string code)
        {
            try 
            {
                SyntaxTree syntaxTree = CSharpSyntaxTree.ParseText(Tools.BuildCode(code));
                string assemblyName = Path.GetRandomFileName();
                MetadataReference[] references = new MetadataReference[]
                {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location)
                };

                CSharpCompilation compilation = CSharpCompilation.Create(
                    assemblyName,
                    syntaxTrees: new[] { syntaxTree },
                    references: references,
                    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

                using (var ms = new MemoryStream())
                {
                    EmitResult result = compilation.Emit(ms);

                    if (!result.Success)
                    {
                        IEnumerable<Diagnostic> failures = result.Diagnostics.Where(diagnostic =>
                            diagnostic.IsWarningAsError ||
                            diagnostic.Severity == DiagnosticSeverity.Error);

                        foreach (Diagnostic diagnostic in failures)
                        {
                            Console.Error.WriteLine("{0}: {1}", diagnostic.Id, diagnostic.GetMessage());
                        }

                        return null;
                    }
                    else
                    {
                        ms.Seek(0, SeekOrigin.Begin);
                        Assembly assembly = Assembly.Load(ms.ToArray());
                        Type type = assembly.GetType("C");
                        object obj = Activator.CreateInstance(type);
                        var a = type.InvokeMember("T",
                            BindingFlags.Default | BindingFlags.InvokeMethod,
                            null,
                            obj,
                            null);

                        return a;
                    }
                }
            }
            catch(Exception ex)
            {
                var a = ex;
                return (object)null;
            }
          
        }

        /*
        public static object ExecuteCsharpCode(string code)
        {
            try
            {
                return new CSharpCodeProvider().CompileAssemblyFromSource(new CompilerParameters(), Tools.BuildCode(code)).CompiledAssembly.GetType("C").GetMethod("T")?.Invoke((object)null, (object[])null);
            }
            catch(Exception EX)
            {
                var  a = EX;
                return (object)null;
            }
        }
        */

        private static string BuildCode(string code)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("using System;");
            stringBuilder.Append("public class C{ public static Object T(){");
            stringBuilder.Append(code);
            stringBuilder.Append("}}");
            return stringBuilder.ToString();
        }

        public static string DataTableToHtml(DataTable dt)
        {
            if (dt.Columns.Count == 0)
                return "";
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("<table class=\"listtable\" style=\"WORD-BREAK:break-all;WORD-WRAP:break-word\">");
            stringBuilder.Append("<thead>");
            stringBuilder.Append("<tr>");
            foreach (DataColumn column in (InternalDataCollectionBase)dt.Columns)
                stringBuilder.Append("<th>" + column.ColumnName + "</th>");
            stringBuilder.Append("</tr>");
            stringBuilder.Append("</thead>");
            stringBuilder.Append("<tbody>");
            foreach (DataRow row in (InternalDataCollectionBase)dt.Rows)
            {
                stringBuilder.Append("<tr>");
                for (int index = 0; index < dt.Columns.Count; ++index)
                    stringBuilder.Append("<td>" + row[index].ToString().HtmlEncode() + "</td>");
                stringBuilder.Append("</tr>");
            }
            stringBuilder.Append("</tbody>");
            stringBuilder.Append("</table>");
            return stringBuilder.ToString();
        }

        public static DateTime JavaLongToDateTime(int time_JAVA_Long)
        {
            return TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1)).Add(new TimeSpan(long.Parse(time_JAVA_Long.ToString() + "0000000")));
        }

        public static string[] GetImageUrlFromHTML(string sHtmlText)
        {
            MatchCollection matchCollection = new Regex("<img\\b[^<>]*?\\bsrc[\\s\\t\\r\\n]*=[\\s\\t\\r\\n]*[\"']?[\\s\\t\\r\\n]*(?<imgUrl>[^\\s\\t\\r\\n\"'<>]*)[^<>]*?/?[\\s\\t\\r\\n]*>", RegexOptions.IgnoreCase).Matches(sHtmlText);
            int num = 0;
            string[] strArray = new string[matchCollection.Count];
            foreach (Match match in matchCollection)
                strArray[num++] = match.Groups["imgUrl"].Value;
            return strArray;
        }

        public static string RemoveHTML(string Htmlstring)
        {
            Htmlstring = Regex.Replace(Htmlstring, "<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "<(.[^>]*)>", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "([\\r\\n])[\\s]+", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "-->", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "<!--.*", "", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(quot|#34);", "\"", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(amp|#38);", "&", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(lt|#60);", "<", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(gt|#62);", ">", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(nbsp|#160);", "   ", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(iexcl|#161);", "¡", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(cent|#162);", "¢", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(pound|#163);", "£", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&(copy|#169);", "©", RegexOptions.IgnoreCase);
            Htmlstring = Regex.Replace(Htmlstring, "&#(\\d+);", "", RegexOptions.IgnoreCase);
            Htmlstring.Replace("<", "");
            Htmlstring.Replace(">", "");
            Htmlstring.Replace("\r\n", "");
            Htmlstring =  Htmlstring.HtmlEncode().Trim();
            return Htmlstring;
        }

        public static bool IsPhoneAccess()
        {
            string serverVariable = HttpContext.Request.UserAgent();
            Regex regex1 = new Regex("(android|bb\\d+|meego).+mobile|avantgo|bada\\/|blackberry|blazer|compal|elaine|fennec|hiptop|iemobile|ip(hone|od)|iris|kindle|lge |maemo|midp|mmp|mobile.+firefox|netfront|opera m(ob|in)i|palm( os)?|phone|p(ixi|re)\\/|plucker|pocket|psp|series(4|6)0|symbian|treo|up\\.(browser|link)|vodafone|wap|windows ce|xda|xiino", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex regex2 = new Regex("1207|6310|6590|3gso|4thp|50[1-6]i|770s|802s|a wa|abac|ac(er|oo|s\\-)|ai(ko|rn)|al(av|ca|co)|amoi|an(ex|ny|yw)|aptu|ar(ch|go)|as(te|us)|attw|au(di|\\-m|r |s )|avan|be(ck|ll|nq)|bi(lb|rd)|bl(ac|az)|br(e|v)w|bumb|bw\\-(n|u)|c55\\/|capi|ccwa|cdm\\-|cell|chtm|cldc|cmd\\-|co(mp|nd)|craw|da(it|ll|ng)|dbte|dc\\-s|devi|dica|dmob|do(c|p)o|ds(12|\\-d)|el(49|ai)|em(l2|ul)|er(ic|k0)|esl8|ez([4-7]0|os|wa|ze)|fetc|fly(\\-|_)|g1 u|g560|gene|gf\\-5|g\\-mo|go(\\.w|od)|gr(ad|un)|haie|hcit|hd\\-(m|p|t)|hei\\-|hi(pt|ta)|hp( i|ip)|hs\\-c|ht(c(\\-| |_|a|g|p|s|t)|tp)|hu(aw|tc)|i\\-(20|go|ma)|i230|iac( |\\-|\\/)|ibro|idea|ig01|ikom|im1k|inno|ipaq|iris|ja(t|v)a|jbro|jemu|jigs|kddi|keji|kgt( |\\/)|klon|kpt |kwc\\-|kyo(c|k)|le(no|xi)|lg( g|\\/(k|l|u)|50|54|\\-[a-w])|libw|lynx|m1\\-w|m3ga|m50\\/|ma(te|ui|xo)|mc(01|21|ca)|m\\-cr|me(rc|ri)|mi(o8|oa|ts)|mmef|mo(01|02|bi|de|do|t(\\-| |o|v)|zz)|mt(50|p1|v )|mwbp|mywa|n10[0-2]|n20[2-3]|n30(0|2)|n50(0|2|5)|n7(0(0|1)|10)|ne((c|m)\\-|on|tf|wf|wg|wt)|nok(6|i)|nzph|o2im|op(ti|wv)|oran|owg1|p800|pan(a|d|t)|pdxg|pg(13|\\-([1-8]|c))|phil|pire|pl(ay|uc)|pn\\-2|po(ck|rt|se)|prox|psio|pt\\-g|qa\\-a|qc(07|12|21|32|60|\\-[2-7]|i\\-)|qtek|r380|r600|raks|rim9|ro(ve|zo)|s55\\/|sa(ge|ma|mm|ms|ny|va)|sc(01|h\\-|oo|p\\-)|sdk\\/|se(c(\\-|0|1)|47|mc|nd|ri)|sgh\\-|shar|sie(\\-|m)|sk\\-0|sl(45|id)|sm(al|ar|b3|it|t5)|so(ft|ny)|sp(01|h\\-|v\\-|v )|sy(01|mb)|t2(18|50)|t6(00|10|18)|ta(gt|lk)|tcl\\-|tdg\\-|tel(i|m)|tim\\-|t\\-mo|to(pl|sh)|ts(70|m\\-|m3|m5)|tx\\-9|up(\\.b|g1|si)|utst|v400|v750|veri|vi(rg|te)|vk(40|5[0-3]|\\-v)|vm40|voda|vulc|vx(52|53|60|61|70|80|81|83|85|98)|w3c(\\-| )|webc|whit|wi(g |nc|nw)|wmlb|wonu|x700|yas\\-|your|zeto|zte\\-", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            Regex regex3 = new Regex(".*wechat.*(\\r\\n)?", RegexOptions.IgnoreCase | RegexOptions.Multiline);
            string input = serverVariable;
            if (!regex1.IsMatch(input) && !regex2.IsMatch(serverVariable.Substring(0, 4)))
                return regex3.IsMatch(serverVariable);
            return true;
        }

        public static string GetDateTimeDifferString(DateTime dt1, DateTime dt2)
        {
            TimeSpan timeSpan = dt2 - dt1;
            int days = timeSpan.Days;
            if (days >= 1)
                return days.ToString() + "天";
            int hours = timeSpan.Hours;
            if (hours >= 1)
                return hours.ToString() + "小时";
            int minutes = timeSpan.Minutes;
            if (minutes >= 1)
                return minutes.ToString() + "分钟";
            int seconds = timeSpan.Seconds;
            if (seconds >= 1)
                return seconds.ToString() + "秒";
            return "";
        }
    }
}
