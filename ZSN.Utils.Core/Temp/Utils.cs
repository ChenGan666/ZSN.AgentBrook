using System;
using System.Drawing;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using System.Web.UI;
using Microsoft.VisualBasic;
using System.Collections;
using System.Net;
using System.Data;
using System.Collections.Generic;
using System.Data.OleDb;
using System.Xml;
using ThoughtWorks.QRCode.Codec;
using System.Drawing.Imaging;

namespace Yannyo.Common
{
	/// <summary>
	/// 工具类
	/// </summary>
    public class Utils
    {
        public const string ASSEMBLY_VERSION = "2.5.0";

        private static Regex RegexBr = new Regex(@"(\r\n)", RegexOptions.IgnoreCase);
        private static Regex RegexBrB = new Regex(@"(\r)", RegexOptions.IgnoreCase);
        private static Regex RegexBrC = new Regex(@"(\n)", RegexOptions.IgnoreCase);
        public static Regex RegexFont = new Regex(@"<font color=" + "\".*?\"" + @">([\s\S]+?)</font>", Utils.GetRegexCompiledOptions());

        private static FileVersionInfo AssemblyFileVersion = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);

        private static string TemplateCookieName = string.Format("templateskinname_{0}_{1}_{2}", AssemblyFileVersion.FileMajorPart, AssemblyFileVersion.FileMinorPart, AssemblyFileVersion.FileBuildPart);



        /// <summary>
        /// 算一个值到另一个值范围内的值
        /// </summary>
        public static float ValueBound(int tValue, int AMin, int AMax, int BMin, int BMax)
        {
            float reValue = 0;
            if (tValue > AMin && tValue < AMax)
            {
                float a = BMax - BMin;
                float b = (float.Parse(tValue.ToString()) - float.Parse(AMin.ToString())) / (float.Parse(AMax.ToString()) - float.Parse(AMin.ToString()));
                float c = a * b + BMin;

                reValue = c;
            }
            else if (tValue <= AMin)
            {
                reValue = BMin;
            }
            else if (tValue >= AMax)
            {
                reValue = BMax;
            }
            return reValue;
        }

        #region 默认返回
        /// <summary>
        /// 返回字符串真实长度, 1个汉字长度为2
        /// </summary>
        /// <returns>字符长度</returns>
        public static int GetStringLength(string str)
        {
            return Encoding.Default.GetBytes(str).Length;
        }
        /// <summary>
        /// 判断指定字符串在指定字符串数组中的位置
        /// </summary>
        /// <param name="strSearch">字符串</param>
        /// <param name="stringArray">字符串数组</param>
        /// <param name="caseInsensetive">是否不区分大小写, true为不区分, false为区分</param>
        /// <returns>字符串在指定字符串数组中的位置, 如不存在则返回-1</returns>
        public static int GetInArrayID(string strSearch, string[] stringArray, bool caseInsensetive)
        {
            for (int i = 0; i < stringArray.Length; i++)
            {
                if (caseInsensetive)
                {
                    if (strSearch.ToLower() == stringArray[i].ToLower())
                    {
                        return i;
                    }
                }
                else
                {
                    if (strSearch == stringArray[i])
                    {
                        return i;
                    }
                }

            }
            return -1;
        }
        /// <summary>
        /// 判断指定字符串在指定字符串数组中的位置
        /// </summary>
        /// <param name="strSearch">字符串</param>
        /// <param name="stringArray">字符串数组</param>
        /// <returns>字符串在指定字符串数组中的位置, 如不存在则返回-1</returns>		
        public static int GetInArrayID(string strSearch, string[] stringArray)
        {
            return GetInArrayID(strSearch, stringArray, true);
        }
        /// <summary>
        /// 获得当前绝对路径
        /// </summary>
        /// <param name="strPath">指定的路径</param>
        /// <returns>绝对路径</returns>
        public static string GetMapPath(string strPath)
        {
            if (HttpContext.Current != null)
            {
                return HttpContext.Current.Server.MapPath(strPath);
            }
            else //非web程序引用
            {
                strPath = strPath.Replace("/", "\\");
                if (strPath.StartsWith("\\"))
                {
                    strPath = strPath.Substring(strPath.IndexOf('\\', 1)).TrimStart('\\');
                }
                return System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, strPath);
            }
        }

        /// <summary>
        /// 获得当前URL
        /// </summary>
        public static string GetUrlReferrer()
        {//返回用户来源,上一页面地址
            string userSource = System.Web.HttpContext.Current.Request.UrlReferrer == null ? "" : System.Web.HttpContext.Current.Request.UrlReferrer.ToString();
            if (userSource == null || userSource == "")
            {
                return "";
            }
            else
            {
                return userSource;
            }
        }
        /// <summary>
        /// 根据Url获得源文件内容
        /// </summary>
        /// <param name="url">合法的Url地址</param>
        /// <returns></returns>
        public static string GetSourceTextByUrl(string url)
        {
            WebRequest request = WebRequest.Create(url);
            request.Timeout = 20000;//20秒超时
            WebResponse response = request.GetResponse();

            Stream resStream = response.GetResponseStream();
            StreamReader sr = new StreamReader(resStream);
            return sr.ReadToEnd();
        }
        /// <summary>
        /// 返回URL中结尾的文件名
        /// </summary>		
        public static string GetFilename(string url)
        {
            if (url == null)
            {
                return "";
            }
            string[] strs1 = url.Split(new char[] { '/' });
            return strs1[strs1.Length - 1].Split(new char[] { '?' })[0];
        }
        /// <summary>
        /// 根据阿拉伯数字返回月份的名称
        /// </summary>	
        public static string[] Monthes
        {
            get
            {
                return new string[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" };
            }
        }
        public static string GetEmailHostName(string strEmail)
        {
            if (strEmail.IndexOf("@") < 0)
            {
                return "";
            }
            return strEmail.Substring(strEmail.LastIndexOf("@")).ToLower();
        }
        /// <summary>
        /// 返回标准日期格式string
        /// </summary>
        public static string GetDate()
        {
            return DateTime.Now.ToString("yyyy-MM-dd");
        }

        /// <summary>
        /// 返回指定日期格式
        /// </summary>
        public static string GetDate(string datetimestr, string replacestr)
        {
            if (datetimestr == null)
            {
                return replacestr;
            }

            if (datetimestr.Equals(""))
            {
                return replacestr;
            }

            try
            {
                datetimestr = Convert.ToDateTime(datetimestr).ToString("yyyy-MM-dd").Replace("1900-01-01", replacestr);
            }
            catch
            {
                return replacestr;
            }
            return datetimestr;
        }


        /// <summary>
        /// 返回标准时间格式string
        /// </summary>
        public static string GetTime()
        {
            return DateTime.Now.ToString("HH:mm:ss");
        }

        /// <summary>
        /// 返回标准时间格式string
        /// </summary>
        public static string GetDateTime()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 返回相对于当前时间的相对天数
        /// </summary>
        public static string GetDateTime(int relativeday)
        {
            return DateTime.Now.AddDays(relativeday).ToString("yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 返回标准时间格式string
        /// </summary>
        public static string GetDateTimeF()
        {
            return DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss:fffffff");
        }

        /// <summary>
        /// 返回标准时间 
        /// </sumary>
        public static string GetStandardDateTime(string fDateTime, string formatStr)
        {
            if (fDateTime == "0000-0-0 0:00:00")
            {
                return fDateTime;
            }
            DateTime s = Convert.ToDateTime(fDateTime);
            return s.ToString(formatStr);
        }

        /// <summary>
        /// 返回标准时间 yyyy-MM-dd HH:mm:ss
        /// </sumary>
        public static string GetStandardDateTime(string fDateTime)
        {
            return GetStandardDateTime(fDateTime, "yyyy-MM-dd HH:mm:ss");
        }

        /// <summary>
        /// 返回标准时间 yyyy-MM-dd
        /// </sumary>
        public static string GetStandardDate(string fDate)
        {
            return GetStandardDateTime(fDate, "yyyy-MM-dd");
        }
        /// <summary>
        /// 时间转成Unix时间戳
        /// </summary>
        /// <param name="dateTime"></param>
        /// <returns></returns>
        public static long UNIX_TIMESTAMP(DateTime dateTime)
        {
            return (dateTime.Ticks - DateTime.Parse("1970-01-01 00:00:00").Ticks) / 10000000;
        }
        /// <summary>
        /// 当前客户端IP
        /// </summary>
        public static string GetRealIP()
        {
            string ip = HTTPRequest.GetIP();

            return ip;
        }

        /// <summary>
        /// 当前客户端系统语言
        /// </summary>
        /// <returns></returns>
        public static string GetRealLanguage()
        {
            string language = HTTPRequest.GetLanguage();
            return language;
        }
        #endregion

        #region QRCode
        /// <summary>
        /// 
        /// </summary>
        /// <param name="data"></param>
        /// <param name="scale">尺寸</param>
        /// <param name="version">版本</param>
        /// <param name="ErrorCorrect">错误纠正</param>
        /// <returns></returns>
        public static Image QRCode(string data,int scale,int version,int ErrorCorrect) {
            QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
            //设置二维码编码格式
            qrCodeEncoder.QRCodeEncodeMode = QRCodeEncoder.ENCODE_MODE.BYTE;
            //设置编码测量度
            qrCodeEncoder.QRCodeScale = scale;
            //设置编码版本
            qrCodeEncoder.QRCodeVersion = version;
            //设置错误校验
            qrCodeEncoder.QRCodeErrorCorrect = QRCodeEncoder.ERROR_CORRECTION.M;

            System.Drawing.Image image;

            image = qrCodeEncoder.Encode(data);

            return image;
        }

        public static byte[] PhotoImageInsert(System.Drawing.Image imgPhoto)
        {
            //将Image转换成流数据，并保存为byte[]   
            MemoryStream mstream = new MemoryStream();
            imgPhoto.Save(mstream, System.Drawing.Imaging.ImageFormat.Bmp);
            byte[] byData = new Byte[mstream.Length];
            mstream.Position = 0;
            mstream.Read(byData, 0, byData.Length);
            mstream.Close();
            return byData;
        }

        /// <summary>
        /// 生成二维码
        /// </summary>
        /// <param name="Content">内容文本</param>
        /// <param name="QRCodeEncodeMode">二维码编码方式</param>
        /// <param name="QRCodeErrorCorrect">纠错码等级</param>
        /// <param name="QRCodeVersion">二维码版本号 0-40</param>
        /// <param name="QRCodeScale">每个小方格的预设宽度（像素），正整数</param>
        /// <param name="size">图片尺寸（像素），0表示不设置</param>
        /// <param name="border">图片白边（像素），当size大于0时有效</param>
        /// <returns></returns>
        public static System.Drawing.Image CreateQRCode(string Content, QRCodeEncoder.ENCODE_MODE QRCodeEncodeMode, QRCodeEncoder.ERROR_CORRECTION QRCodeErrorCorrect, int QRCodeVersion, int QRCodeScale, int size, int border)
        {
            QRCodeEncoder qrCodeEncoder = new QRCodeEncoder();
            qrCodeEncoder.QRCodeEncodeMode = QRCodeEncodeMode;
            qrCodeEncoder.QRCodeErrorCorrect = QRCodeErrorCorrect;
            qrCodeEncoder.QRCodeScale = QRCodeScale;
            qrCodeEncoder.QRCodeVersion = QRCodeVersion;
            System.Drawing.Image image = qrCodeEncoder.Encode(Content);

            #region 根据设定的目标图片尺寸调整二维码QRCodeScale设置，并添加边框
            if (size > 0)
            {

                
                //当设定目标图片尺寸大于生成的尺寸时，逐步增大方格尺寸
                #region 当设定目标图片尺寸大于生成的尺寸时，逐步增大方格尺寸
                while (image.Width < size)
                {
                    qrCodeEncoder.QRCodeScale++;
                    System.Drawing.Image imageNew = qrCodeEncoder.Encode(Content);
                    if (imageNew.Width < size)
                    {
                        image = new System.Drawing.Bitmap(imageNew);
                        imageNew.Dispose();
                        imageNew = null;
                    }
                    else
                    {
                        qrCodeEncoder.QRCodeScale--; //新尺寸未采用，恢复最终使用的尺寸
                        imageNew.Dispose();
                        imageNew = null;
                        break;
                    }
                }
                #endregion

                
                //当设定目标图片尺寸小于生成的尺寸时，逐步减小方格尺寸
                #region 当设定目标图片尺寸小于生成的尺寸时，逐步减小方格尺寸
                while (image.Width > size && qrCodeEncoder.QRCodeScale > 1)
                {
                    qrCodeEncoder.QRCodeScale--;
                    System.Drawing.Image imageNew = qrCodeEncoder.Encode(Content);
                    image = new System.Drawing.Bitmap(imageNew);
                    imageNew.Dispose();
                    imageNew = null;
                    if (image.Width < size)
                    {
                        break;
                    }
                }
                #endregion
                
                //如果目标尺寸大于生成的图片尺寸，则为图片增加白边
                #region 如果目标尺寸大于生成的图片尺寸，则为图片增加白边
                if (image.Width <= size)
                {
                    //根据参数设置二维码图片白边的最小宽度
                    #region 根据参数设置二维码图片白边的最小宽度
                    if (border > 0)
                    {
                        while (image.Width <= size && size - image.Width < border * 2 && qrCodeEncoder.QRCodeScale > 1)
                        {
                            qrCodeEncoder.QRCodeScale--;
                            System.Drawing.Image imageNew = qrCodeEncoder.Encode(Content);
                            image = new System.Drawing.Bitmap(imageNew);
                            imageNew.Dispose();
                            imageNew = null;
                        }
                    }
                    #endregion

                    //当目标图片尺寸大于二维码尺寸时，将二维码绘制在目标尺寸白色画布的中心位置
                    if (image.Width < size)
                    {
                        //新建空白绘图
                        System.Drawing.Bitmap panel = new System.Drawing.Bitmap(size, size);
                        System.Drawing.Graphics graphic0 = System.Drawing.Graphics.FromImage(panel);
                        int p_left = 0;
                        int p_top = 0;
                        if (image.Width <= size) //如果原图比目标形状宽
                        {
                            p_left = (size - image.Width) / 2;
                        }
                        if (image.Height <= size)
                        {
                            p_top = (size - image.Height) / 2;
                        }

                        //将生成的二维码图像粘贴至绘图的中心位置
                        graphic0.DrawImage(image, p_left, p_top, image.Width, image.Height);
                        image = new System.Drawing.Bitmap(panel);
                        panel.Dispose();
                        panel = null;
                        graphic0.Dispose();
                        graphic0 = null;
                    }
                }
                #endregion
            }
            #endregion
            return image;
        }

        private static bool IsTrue() // 在Image类别对图片进行缩放的时候,需要一个返回bool类别的委托 
        {
            return true;
        }


        #endregion

        #region 图片特效

        //锐化
        public static Bitmap SharpenImage(Bitmap bmp)
        {
            int height = bmp.Height;
            int width = bmp.Width;
            Bitmap newbmp = new Bitmap(width, height, PixelFormat.Format24bppRgb);

            LockBitmap lbmp = new LockBitmap(bmp);
            LockBitmap newlbmp = new LockBitmap(newbmp);
            lbmp.LockBits();
            newlbmp.LockBits();

            Color pixel;
            //拉普拉斯模板
            int[] Laplacian = { -1, -1, -1, -1, 9, -1, -1, -1, -1 };
            for (int x = 1; x < width - 1; x++)
            {
                for (int y = 1; y < height - 1; y++)
                {
                    int r = 0, g = 0, b = 0;
                    int Index = 0;
                    for (int col = -1; col <= 1; col++)
                    {
                        for (int row = -1; row <= 1; row++)
                        {
                            pixel = lbmp.GetPixel(x + row, y + col); r += pixel.R * Laplacian[Index];
                            g += pixel.G * Laplacian[Index];
                            b += pixel.B * Laplacian[Index];
                            Index++;
                        }
                    }
                    //处理颜色值溢出
                    r = r > 255 ? 255 : r;
                    r = r < 0 ? 0 : r;
                    g = g > 255 ? 255 : g;
                    g = g < 0 ? 0 : g;
                    b = b > 255 ? 255 : b;
                    b = b < 0 ? 0 : b;
                    newlbmp.SetPixel(x - 1, y - 1, Color.FromArgb(r, g, b));
                }
            }
            lbmp.UnlockBits();
            newlbmp.UnlockBits();
            return newbmp;
        }
        #endregion

        #region SQL语句处理
        /// <summary>
        /// 去除字符串中的最后一个分隔符
        /// </summary>
        public static string ReStrCheckPo(string cStr, string gP)
        {
            if (cStr.Trim() != "")
            {
                if (cStr.Substring(cStr.Length - 1, 1) == gP)
                {
                    cStr = cStr.Substring(0, cStr.Length - 1);
                }
            }
            return cStr;
        }
        /// <summary>
        /// 返回整理后的SQL数据集合,例子:1,2,3,4
        /// </summary>
        public static string ReSQLSetTxt(string SetTxt)
        {
            string tRe = "";
            if (SetTxt.IndexOf(",") > -1)
            {
                string[] tV = SetTxt.Split(new Char[] { ',' });
                if (tV.Length > 0)
                {
                    for (int i = 0; i < tV.Length; i++)
                    {
                        if (tV[i] != null)
                        {
                            if (tV[i].Trim() != "")
                            {
                                tRe += tV[i].Trim() + ",";
                            }
                        }
                    }
                }
            }
            if (tRe.Trim() != "")
            {
                tRe = ReStrCheckPo(tRe, ",");
            }
            else
            {
                tRe = SetTxt;
            }
            return tRe;
        }

        /// <summary>
        /// 返回整理后的SQL数据集合,例子:1,2,3,4
        /// </summary>
        /// <param name="SetTxt">原字符串</param>
        /// <param name="aStr">单字符前后增加字符</param>
        public static string ReSQLSetTxt(string SetTxt, string aStr)
        {
            string tRe = "";
            if (SetTxt.IndexOf(",") > -1)
            {
                string[] tV = SetTxt.Split(new Char[] { ',' });
                if (tV.Length > 0)
                {
                    for (int i = 0; i < tV.Length; i++)
                    {
                        if (tV[i] != null)
                        {
                            if (tV[i].Trim() != "")
                            {
                                tRe += aStr + tV[i].Trim() + aStr + ",";
                            }
                        }
                    }
                }
            }
            if (tRe.Trim() != "")
            {
                tRe = ReStrCheckPo(tRe, ",");
            }
            else
            {
                tRe = SetTxt;
            }
            return tRe;
        }

        /// <summary>
        /// 返回整理后的SQL数据集合,例子:1,2,3,4
        /// </summary>
        public static string ReSQLSetTxt(string SetTxt, string gP, string tP)
        {
            string tRe = "";
            if (SetTxt.IndexOf(",") > -1)
            {
                string[] tV = SetTxt.Split(gP.ToCharArray());
                if (tV.Length > 0)
                {
                    for (int i = 0; i < tV.Length; i++)
                    {
                        if (tV[i] != null)
                        {
                            if (tV[i].Trim() != "")
                            {
                                tRe += tV[i].Trim() + tP;
                            }
                        }
                    }
                }
            }
            if (tRe.Trim() != "")
            {
                tRe = ReStrCheckPo(tRe, tP);
            }
            return tRe;
        }
        /// <summary>
        /// 改正sql语句中的转义字符
        /// </summary>
        public static string mashSQL(string str)
        {
            string str2;

            if (str == null)
            {
                str2 = "";
            }
            else
            {
                str = str.Replace("\'", "'");
                str2 = str;
            }
            return str2;
        }

        /// <summary>
        /// 替换sql语句中的有问题符号
        /// </summary>
        public static string ChkSQL(string str)
        {
            string str2;

            if (str == null)
            {
                str2 = "";
            }
            else
            {
                str = str.Replace("'", "''");
                str2 = str;
            }
            return str2;
        }
        #endregion

        #region Assembly 信息部分
        /// <summary>
        /// 获得Assembly版本号
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblyVersion()
        {
            return string.Format("{0}.{1}.{2}", AssemblyFileVersion.FileMajorPart, AssemblyFileVersion.FileMinorPart, AssemblyFileVersion.FileBuildPart);
        }

        /// <summary>
        /// 获得Assembly产品名称
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblyProductName()
        {
            return AssemblyFileVersion.ProductName;
        }

        /// <summary>
        /// 获得Assembly产品版权
        /// </summary>
        /// <returns></returns>
        public static string GetAssemblyCopyright()
        {
            return AssemblyFileVersion.LegalCopyright;
        }
        #endregion

        #region Cookie操作
        /// <summary>
        /// 写cookie值
        /// </summary>
        /// <param name="strName">名称</param>
        /// <param name="strValue">值</param>
        public static void WriteCookie(string strName, string strValue)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[strName];
            if (cookie == null)
            {
                cookie = new HttpCookie(strName);
            }
            cookie.Value = strValue;
            HttpContext.Current.Response.AppendCookie(cookie);

        }

        /// <summary>
        /// 写cookie值
        /// </summary>
        /// <param name="strName">名称</param>
        /// <param name="strValue">值</param>
        public static void WriteCookie(string strName, string key, string strValue)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[strName];
            if (cookie == null)
            {
                cookie = new HttpCookie(strName);
            }
            cookie[key] = strValue;
            HttpContext.Current.Response.AppendCookie(cookie);

        }
        /// <summary>
        /// 写cookie值
        /// </summary>
        /// <param name="strName">名称</param>
        /// <param name="strValue">值</param>
        public static void WriteCookie(string strName, string key, string strValue, int expires)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[strName];
            if (cookie == null)
            {
                cookie = new HttpCookie(strName);
            }
            cookie[key] = strValue;
            cookie.Expires = DateTime.Now.AddMinutes(expires);
            HttpContext.Current.Response.AppendCookie(cookie);

        }
        /// <summary>
        /// 写cookie值
        /// </summary>
        /// <param name="strName">名称</param>
        /// <param name="strValue">值</param>
        /// <param name="strValue">过期时间(分钟)</param>
        public static void WriteCookie(string strName, string strValue, int expires)
        {
            HttpCookie cookie = HttpContext.Current.Request.Cookies[strName];
            if (cookie == null)
            {
                cookie = new HttpCookie(strName);
            }
            cookie.Value = strValue;
            cookie.Expires = DateTime.Now.AddMinutes(expires);
            HttpContext.Current.Response.AppendCookie(cookie);

        }

        /// <summary>
        /// 读cookie值
        /// </summary>
        /// <param name="strName">名称</param>
        /// <returns>cookie值</returns>
        public static string GetCookie(string strName)
        {
            if (HttpContext.Current.Request.Cookies != null && HttpContext.Current.Request.Cookies[strName] != null)
            {
                return HttpContext.Current.Request.Cookies[strName].Value.ToString();
            }

            return "";
        }

        /// <summary>
        /// 读cookie值
        /// </summary>
        /// <param name="strName">名称</param>
        /// <returns>cookie值</returns>
        public static string GetCookie(string strName, string key)
        {
            if (HttpContext.Current.Request.Cookies != null && HttpContext.Current.Request.Cookies[strName] != null && HttpContext.Current.Request.Cookies[strName][key] != null)
            {
                return HttpContext.Current.Request.Cookies[strName][key].ToString();
            }
            return "";
        }
        /// <summary>
        /// 清除Cookies
        /// </summary>
        /// <param name="cookieName">Cookies类名</param>
        /// <param name="cookieDomain">域</param>
        public static void ClearCookie(string cookieName, string cookieDomain)
        {
            HttpCookie cookie = new HttpCookie(cookieName);
            cookie.Values.Clear();
            cookie.Expires = DateTime.Now.AddYears(-1);
            if (cookieDomain != string.Empty && HttpContext.Current.Request.Url.Host.IndexOf(cookieDomain) > -1 && IsValidDomain(HttpContext.Current.Request.Url.Host))
                cookie.Domain = cookieDomain;
            HttpContext.Current.Response.AppendCookie(cookie);
        }

        /// <summary>
        /// 从cookie中获取转向页
        /// </summary>
        /// <returns>string</returns>
        public static string GetReUrl(string DefaultURL)
        {
            if (HTTPRequest.GetString("reurl").Trim() != "")
            {
                WriteCookie("reurl", HTTPRequest.GetString("reurl").Trim());
                return HTTPRequest.GetString("reurl").Trim();
            }
            else
            {
                if (GetCookie("reurl") == "")
                {
                    return DefaultURL;
                }
                else
                {
                    return GetCookie("reurl");
                }
            }
        }
        #endregion

        #region 时间处理
        /// <summary>
        /// 返回相差的秒数
        /// </summary>
        /// <param name="Time"></param>
        /// <param name="Sec"></param>
        /// <returns></returns>
        public static int StrDateDiffSeconds(string Time, int Sec)
        {
            TimeSpan ts = DateTime.Now - DateTime.Parse(Time).AddSeconds(Sec);
            if (ts.TotalSeconds > int.MaxValue)
            {
                return int.MaxValue;
            }
            else if (ts.TotalSeconds < int.MinValue)
            {
                return int.MinValue;
            }
            return (int)ts.TotalSeconds;
        }

        /// <summary>
        /// 返回相差的分钟数
        /// </summary>
        /// <param name="time"></param>
        /// <param name="minutes"></param>
        /// <returns></returns>
        public static int StrDateDiffMinutes(string time, int minutes)
        {
            if (time == "" || time == null)
                return 1;
            TimeSpan ts = DateTime.Now - DateTime.Parse(time).AddMinutes(minutes);
            if (ts.TotalMinutes > int.MaxValue)
            {
                return int.MaxValue;
            }
            else if (ts.TotalMinutes < int.MinValue)
            {
                return int.MinValue;
            }
            return (int)ts.TotalMinutes;
        }

        /// <summary>
        /// 返回相差的小时数
        /// </summary>
        /// <param name="time"></param>
        /// <param name="hours"></param>
        /// <returns></returns>
        public static int StrDateDiffHours(string time, int hours)
        {
            if (time == "" || time == null)
                return 1;
            TimeSpan ts = DateTime.Now - DateTime.Parse(time).AddHours(hours);
            if (ts.TotalHours > int.MaxValue)
            {
                return int.MaxValue;
            }
            else if (ts.TotalHours < int.MinValue)
            {
                return int.MinValue;
            }
            return (int)ts.TotalHours;
        }
        /// <summary>
        /// 当前时间加分钟
        /// </summary>
        /// <param name="times"></param>
        /// <returns></returns>
        public static string AdDeTime(int times)
        {
            string newtime = (DateTime.Now).AddMinutes(times).ToString();
            return newtime;

        }

        /// <summary>
        /// 计算两个时间差,并返回 x天/x小时/x分钟/x秒
        /// </summary>
        public static string SubtractTime(DateTime OneTime, DateTime TowTime)
        {
            TimeSpan span = OneTime.Subtract(TowTime);
            if (span.Days > 0)
            {
                return span.Days + "天";
            }
            else if (span.Hours > 0)
            {
                return span.Hours + "小时";
            }
            else if (span.Minutes > 0)
            {
                return span.Minutes + "分钟";
            }
            else if (span.Seconds > 0)
            {
                return span.Seconds + "秒";
            }
            else
            {
                return "3秒";
            }
        }
        #endregion

        #region 文件操作

        /// <summary>
        /// 写日志文件
        /// </summary>
        /// <param name="files"></param>
        /// <param name="logstr"></param>
        public static void WriteLog(string files,string logstr)
        {
            string sss = files;
            FileStream fst = new FileStream(sss, FileMode.Append);
            StreamWriter swt = new StreamWriter(fst, System.Text.Encoding.GetEncoding("utf-8"));
            swt.WriteLine(DateTime.Now+"    "+ logstr+"\r\n");
            swt.Close();
            fst.Close();
        }

        public static void WriteLog4Net() {

        }

        /// <summary>
        /// 以指定的ContentType输出指定文件文件
        /// </summary>
        /// <param name="filepath">文件路径</param>
        /// <param name="filename">输出的文件名</param>
        /// <param name="filetype">将文件输出时设置的ContentType</param>
        public static void ResponseFile(string filepath, string filename, string filetype)
        {
            Stream iStream = null;

            // 缓冲区为10k
            byte[] buffer = new Byte[10000];

            // 文件长度
            int length;

            // 需要读的数据长度
            long dataToRead;

            try
            {
                // 打开文件
                iStream = new FileStream(filepath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);


                // 需要读的数据长度
                dataToRead = iStream.Length;

                HttpContext.Current.Response.ContentType = filetype;
                HttpContext.Current.Response.AddHeader("Content-Disposition", "attachment;filename=" + Utils.UrlEncode(filename.Trim()).Replace("+", " "));

                while (dataToRead > 0)
                {
                    // 检查客户端是否还处于连接状态
                    if (HttpContext.Current.Response.IsClientConnected)
                    {
                        length = iStream.Read(buffer, 0, 10000);
                        HttpContext.Current.Response.OutputStream.Write(buffer, 0, length);
                        HttpContext.Current.Response.Flush();
                        buffer = new Byte[10000];
                        dataToRead = dataToRead - length;
                    }
                    else
                    {
                        // 如果不再连接则跳出死循环
                        dataToRead = -1;
                    }
                }
            }
            catch (Exception ex)
            {
                HttpContext.Current.Response.Write("Error : " + ex.Message);
            }
            finally
            {
                if (iStream != null)
                {
                    // 关闭文件
                    iStream.Close();
                }
            }
            HttpContext.Current.Response.End();
        }
        /// <summary>
        /// 返回文件是否存在
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns>是否存在</returns>
        public static bool FileExists(string filename)
        {
            return System.IO.File.Exists(filename);
        }
        /// <summary>
        /// 转换为静态html
        /// </summary>
        public void transHtml(string path, string outpath)
        {
            Page page = new Page();
            StringWriter writer = new StringWriter();
            page.Server.Execute(path, writer);
            FileStream fs;
            if (File.Exists(page.Server.MapPath("") + "\\" + outpath))
            {
                File.Delete(page.Server.MapPath("") + "\\" + outpath);
                fs = File.Create(page.Server.MapPath("") + "\\" + outpath);
            }
            else
            {
                fs = File.Create(page.Server.MapPath("") + "\\" + outpath);
            }
            byte[] bt = Encoding.Default.GetBytes(writer.ToString());
            fs.Write(bt, 0, bt.Length);
            fs.Close();
        }
        /// <summary>
        /// 返回指定目录下的非 UTF8 字符集文件
        /// </summary>
        /// <param name="Path">路径</param>
        /// <returns>文件名的字符串数组</returns>
        public static string[] FindNoUTF8File(string Path)
        {
            //System.IO.StreamReader reader = null;
            StringBuilder filelist = new StringBuilder();
            DirectoryInfo Folder = new DirectoryInfo(Path);
            //System.IO.DirectoryInfo[] subFolders = Folder.GetDirectories(); 
            /*
            for (int i=0;i<subFolders.Length;i++) 
            { 
                FindNoUTF8File(subFolders[i].FullName); 
            }
            */
            FileInfo[] subFiles = Folder.GetFiles();
            for (int j = 0; j < subFiles.Length; j++)
            {
                if (subFiles[j].Extension.ToLower().Equals(".htm"))
                {
                    FileStream fs = new FileStream(subFiles[j].FullName, FileMode.Open, FileAccess.Read);
                    bool bUtf8 = IsUTF8(fs);
                    fs.Close();
                    if (!bUtf8)
                    {
                        filelist.Append(subFiles[j].FullName);
                        filelist.Append("\r\n");
                    }
                }
            }
            return Utils.SplitString(filelist.ToString(), "\r\n");

        }
        /// <summary>
        /// 创建目录
        /// </summary>
        /// <param name="name">名称</param>
        /// <returns>创建是否成功</returns>
        [DllImport("dbgHelp", SetLastError = true)]
        private static extern bool MakeSureDirectoryPathExists(string name);
        /// <summary>
        /// 建立文件夹
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static bool CreateDir(string name)
        {
            return Utils.MakeSureDirectoryPathExists(name);
        }
        /// <summary>
        /// 得到系统的真实路径
        /// </summary>
        /// <returns></returns>
        public static string GetTrueSysPath()
        {
            string sysPath = HttpContext.Current.Request.Path;
            if (sysPath.LastIndexOf("/") != sysPath.IndexOf("/"))
            {
                sysPath = sysPath.Substring(sysPath.IndexOf("/"), sysPath.LastIndexOf("/") + 1);
            }
            else
            {
                sysPath = "/";
            }
            return sysPath;

        }
        /// <summary>
        /// 备份文件
        /// </summary>
        /// <param name="sourceFileName">源文件名</param>
        /// <param name="destFileName">目标文件名</param>
        /// <param name="overwrite">当目标文件存在时是否覆盖</param>
        /// <returns>操作是否成功</returns>
        public static bool BackupFile(string sourceFileName, string destFileName, bool overwrite)
        {
            if (!System.IO.File.Exists(sourceFileName))
            {
                throw new FileNotFoundException(sourceFileName + "文件不存在！");
            }
            if (!overwrite && System.IO.File.Exists(destFileName))
            {
                return false;
            }
            try
            {
                System.IO.File.Copy(sourceFileName, destFileName, true);
                return true;
            }
            catch (Exception e)
            {
                throw e;
            }
        }


        /// <summary>
        /// 备份文件,当目标文件存在时覆盖
        /// </summary>
        /// <param name="sourceFileName">源文件名</param>
        /// <param name="destFileName">目标文件名</param>
        /// <returns>操作是否成功</returns>
        public static bool BackupFile(string sourceFileName, string destFileName)
        {
            return BackupFile(sourceFileName, destFileName, true);
        }


        /// <summary>
        /// 恢复文件
        /// </summary>
        /// <param name="backupFileName">备份文件名</param>
        /// <param name="targetFileName">要恢复的文件名</param>
        /// <param name="backupTargetFileName">要恢复文件再次备份的名称,如果为null,则不再备份恢复文件</param>
        /// <returns>操作是否成功</returns>
        public static bool RestoreFile(string backupFileName, string targetFileName, string backupTargetFileName)
        {
            try
            {
                if (!System.IO.File.Exists(backupFileName))
                {
                    throw new FileNotFoundException(backupFileName + "文件不存在！");
                }
                if (backupTargetFileName != null)
                {
                    if (!System.IO.File.Exists(targetFileName))
                    {
                        throw new FileNotFoundException(targetFileName + "文件不存在！无法备份此文件！");
                    }
                    else
                    {
                        System.IO.File.Copy(targetFileName, backupTargetFileName, true);
                    }
                }
                System.IO.File.Delete(targetFileName);
                System.IO.File.Copy(backupFileName, targetFileName);
            }
            catch (Exception e)
            {
                throw e;
            }
            return true;
        }
        /// <summary>
        /// 恢复文件
        /// </summary>
        /// <param name="backupFileName"></param>
        /// <param name="targetFileName"></param>
        /// <returns></returns>
        public static bool RestoreFile(string backupFileName, string targetFileName)
        {
            return RestoreFile(backupFileName, targetFileName, null);
        }

        /// <summary>
        /// 获取记录模板id的cookie名称
        /// </summary>
        /// <returns></returns>
        public static string GetTemplateCookieName()
        {
            return TemplateCookieName;
        }

        /// <summary>
        /// 转换长文件名为短文件名
        /// </summary>
        /// <param name="filename"></param>
        /// <param name="repstring"></param>
        /// <param name="leftnum"></param>
        /// <param name="rightnum"></param>
        /// <param name="charnum"></param>
        /// <returns></returns>
        public static string ConvertSimpleFileName(string fullname, string repstring, int leftnum, int rightnum, int charnum)
        {
            string simplefilename = "", leftstring = "", rightstring = "", filename = "";

            string extname = GetFileExtName(fullname);
            if (extname == "" || extname == null)
            {

                throw new Exception("字符串不含有扩展名信息");
            }
            int filelength = 0, dotindex = 0;

            dotindex = fullname.LastIndexOf('.');
            filename = fullname.Substring(0, dotindex);
            filelength = filename.Length;
            if (dotindex > charnum)
            {
                leftstring = filename.Substring(0, leftnum);
                rightstring = filename.Substring(filelength - rightnum, rightnum);
                if (repstring == "" || repstring == null)
                {
                    simplefilename = leftstring + rightstring + "." + extname;
                }
                else
                {
                    simplefilename = leftstring + repstring + rightstring + "." + extname;
                }
            }
            else
            {

                simplefilename = fullname;
            }
            return simplefilename;

        }

        public static string GetFileExtName(string filename)
        {
            string[] array = filename.Trim().Split('.');
            Array.Reverse(array);
            return array[0].ToString();
        }
        #endregion

        #region 数据格式处理

        /// <summary>
        /// 数据类型
        /// </summary>
        /// <param name="_t">
        /// txt: { name: '文本', value: 1},
        /// num: {name: '数字',value: 2},
        /// date: {name: '日期',value: 3},
        /// time:{name:'时间',value:17},
        /// datetime:{name:'日期 时间',value:18},
        /// pic: {name: '图片',value: 4},
        /// movie: {name: '视频',value: 5},
        /// music: {name: '音频',value: 6},
        /// tel: {name: '电话号码',value: 7},
        /// mobile: {name: '手机号码',value: 8},
        /// map: {name: '经纬度(地图)',value: 9},
        /// url: {name: '网址(URL)',value: 10},
        /// email: {name: '邮箱(Email)',value: 11},
        /// idcard: {name: '身份证',value: 12},
        /// select: {name: '下拉框',value: 13},
        /// checkbox: {name: '复选框',value: 14},
        /// radio: {name: '单选框',value: 15},
        /// ip: {name: 'IP地址',value: 16}
        /// </param>
        /// <returns></returns>
        public static Type ToType(int _t)
        {
            Type tp;
            switch (_t)
            {
                case 1:case 4:case 5:case 6:case 7:case 8:case 9:case 10:case 11:case 12:case 13:case 14:case 15:case 16:case 3:case 17:
                    tp = Type.GetType("System.String");
                    break;
                case 2:
                    tp = Type.GetType("System.Numeric");
                    break;
                case 18:
                    tp = Type.GetType("System.DateTime");
                    break;
                default:
                    tp= Type.GetType("System.String");
                    break;
            }
            return tp;
        }

        /// <summary>
        /// 对象转对象
        /// </summary>
        /// <param name="_t"></param>
        /// <param name="_o"></param>
        /// <returns></returns>
        public static Object ToObject(int _t,object _o)
        {
            Object _tO;
            switch (_t)
            {
                case 1:
                case 4:
                case 5:
                case 6:
                case 7:
                case 8:
                case 9:
                case 10:
                case 11:
                case 12:
                case 13:
                case 14:
                case 15:
                case 16:
                case 3:
                case 17:
                    _tO = _o.ToString();
                    break;
                case 2:
                    _tO = Convert.ToDecimal(_o);
                    break;
                case 18:
                    _tO = Convert.ToDateTime(_o);
                    break;
                default:
                    _tO = _o.ToString();
                    break;
            }
            return _tO;
        }

        /// <summary>
        /// 将集合类转换成DataTable
        /// </summary>
        /// <param name="list">集合</param>
        /// <returns></returns>
        public static DataTable ToDataTable(IList list)
        {
            DataTable result = new DataTable();
            if (list.Count > 0)
            {
                PropertyInfo[] propertys = list[0].GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    result.Columns.Add(pi.Name, pi.PropertyType);
                }

                for (int i = 0; i < list.Count; i++)
                {
                    ArrayList tempList = new ArrayList();
                    foreach (PropertyInfo pi in propertys)
                    {
                        object obj = pi.GetValue(list[i], null);
                        tempList.Add(obj);
                    }
                    object[] array = tempList.ToArray();
                    result.LoadDataRow(array, true);
                }
            }
            return result;
        }

        /// <summary>
        /// 将泛型集合类转换成DataTable
        /// </summary>
        /// <typeparam name="T">集合项类型</typeparam>
        /// <param name="list">集合</param>
        /// <returns>数据集(表)</returns>
        public static DataTable ToDataTable<T>(IList<T> list)
        {
            return ToDataTable<T>(list, null);
        }



        /// <summary>
        /// 将泛型集合类转换成DataTable
        /// </summary>
        /// <typeparam name="T">集合项类型</typeparam>
        /// <param name="list">集合</param>
        /// <param name="propertyName">需要返回的列的列名</param>
        /// <returns>数据集(表)</returns>
        public static DataTable ToDataTable<T>(IList<T> list, params string[] propertyName)
        {
            List<string> propertyNameList = new List<string>();
            if (propertyName != null)
                propertyNameList.AddRange(propertyName);

            DataTable result = new DataTable();
            if (list.Count > 0)
            {
                PropertyInfo[] propertys = list[0].GetType().GetProperties();
                foreach (PropertyInfo pi in propertys)
                {
                    if (propertyNameList.Count == 0)
                    {
                        result.Columns.Add(pi.Name, pi.PropertyType);
                    }
                    else
                    {
                        if (propertyNameList.Contains(pi.Name))
                            result.Columns.Add(pi.Name, pi.PropertyType);
                    }
                }

                for (int i = 0; i < list.Count; i++)
                {
                    ArrayList tempList = new ArrayList();
                    foreach (PropertyInfo pi in propertys)
                    {
                        if (propertyNameList.Count == 0)
                        {
                            object obj = pi.GetValue(list[i], null);
                            tempList.Add(obj);
                        }
                        else
                        {
                            if (propertyNameList.Contains(pi.Name))
                            {
                                object obj = pi.GetValue(list[i], null);
                                tempList.Add(obj);
                            }
                        }
                    }
                    object[] newRow = tempList.ToArray();

                    result.BeginLoadData();
                    result.LoadDataRow(newRow, true);
                    result.EndLoadData();
                }
            }
            return result;
        }


        public static string DataSetToJSON(System.Data.DataSet ds)
        {
            if (ds == null || ds.Tables.Count <= 0 || ds.Tables[0].Rows.Count <= 0)
            {
                return "{\"ok\":false}";
            }
            else {
                StringBuilder sb = new StringBuilder();
                sb.Append("{\"ok\":true,table:{");
                foreach (DataTable dt in ds.Tables)
                {
                    sb.Append(string.Format("\"{0}\":[", dt.TableName.ToString().Trim().Replace("\\", "\\\\").Replace("'", "\\'").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{").Replace("}", "\\}")));
                    sb.Append(DataTableToJSON(dt)+"],");
                }
                sb.Remove(sb.ToString().LastIndexOf(','), 1);
                sb.Append("}}");
                return sb.ToString();
            }
        }

        /// <summary>
        /// 将数据表转换成JSON类型串
        /// </summary>
        /// <param name="dt">要转换的数据表</param>
        /// <returns></returns>
        public static StringBuilder DataTableToJSON(System.Data.DataTable dt)
        {
            return DataTableToJson(dt, true);
        }

        /// <summary>
        /// 将数据表转换成JSON类型串
        /// </summary>
        /// <param name="dt">要转换的数据表</param>
        /// <param name="dispose">数据表转换结束后是否dispose掉</param>
        /// <returns></returns>
        public static StringBuilder DataTableToJson(System.Data.DataTable dt, bool dt_dispose)
        {
            StringBuilder stringBuilder = new StringBuilder();
            stringBuilder.Append("[\r\n");

            //数据表字段名和类型数组
            string[] dt_field = new string[dt.Columns.Count];
            int i = 0;
            string formatStr = "{{";
            string fieldtype = "";
            foreach (System.Data.DataColumn dc in dt.Columns)
            {
                dt_field[i] = dc.Caption.ToLower().Trim();
                formatStr += "'" + dc.Caption.ToLower().Trim() + "':";
                fieldtype = dc.DataType.ToString().Trim().ToLower();
                if (fieldtype.IndexOf("int") > 0 || fieldtype.IndexOf("deci") > 0 ||
                    fieldtype.IndexOf("floa") > 0 || fieldtype.IndexOf("doub") > 0 ||
                    fieldtype.IndexOf("bool") > 0)
                {
                    formatStr += "{" + i + "}";
                }
                else
                {
                    formatStr += "'{" + i + "}'";
                }
                formatStr += ",";
                i++;
            }

            if (formatStr.EndsWith(","))
            {
                formatStr = formatStr.Substring(0, formatStr.Length - 1);//去掉尾部","号
            }
            formatStr += "}},";

            i = 0;
            object[] objectArray = new object[dt_field.Length];
            foreach (System.Data.DataRow dr in dt.Rows)
            {
                foreach (string fieldname in dt_field)
                {   //对 \ , ' 符号进行转换 
                    objectArray[i] = dr[dt_field[i]].ToString().Trim().Replace("\\", "\\\\").Replace("'", "\\'").Replace("[", "\\[").Replace("]", "\\]").Replace("{", "\\{").Replace("}", "\\}");
                    switch (objectArray[i].ToString())
                    {
                        case "True":
                            {
                                objectArray[i] = "true"; break;
                            }
                        case "False":
                            {
                                objectArray[i] = "false"; break;
                            }
                        default: break;
                    }
                    i++;
                }
                i = 0;
                stringBuilder.Append(string.Format(formatStr, objectArray));
            }
            if (stringBuilder.ToString().EndsWith(","))
            {
                stringBuilder.Remove(stringBuilder.Length - 1, 1);//去掉尾部","号
            }

            if (dt_dispose)
            {
                dt.Dispose();
            }
            return stringBuilder.Append("\r\n];");
        }

        /// <summary>
        /// DataReader转换为DataTable
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static System.Data.DataTable DataReaderToDataTable(System.Data.IDataReader reader)
        {
            DataTable objDataTable = new DataTable();
            int intFieldCount = reader.FieldCount;
            for (int intCounter = 0; intCounter < intFieldCount; ++intCounter)
            {
                objDataTable.Columns.Add(reader.GetName(intCounter), reader.GetFieldType(intCounter));
            }

            objDataTable.BeginLoadData();

            object[] objValues = new object[intFieldCount];
            while (reader.Read())
            {
                reader.GetValues(objValues);
                objDataTable.LoadDataRow(objValues, true);
            }
            reader.Close();
            objDataTable.EndLoadData();

            return objDataTable;
        }
        #endregion

        #region 校验
        /// <summary>
        /// 是否包含指定字符串
        /// </summary>
        /// <param name="str">源字符串</param>
        /// <param name="stringarray">校验字符串</param>
        /// <param name="strsplit">校验字符串分隔符</param>
        /// <returns></returns>
        public static bool IsCompriseStr(string str, string stringarray, string strsplit)
        {
            if (stringarray == "" || stringarray == null)
            {
                return false;
            }

            str = str.ToLower();
            string[] stringArray = Utils.SplitString(stringarray.ToLower(), strsplit);
            for (int i = 0; i < stringArray.Length; i++)
            {
                //string t1 = str;
                //string t2 = stringArray[i];
                if (str.IndexOf(stringArray[i]) > -1)
                {
                    return true;
                }
            }
            return false;
        }
        /// <summary>
        /// 判断指定字符串是否属于指定字符串数组中的一个元素
        /// </summary>
        /// <param name="strSearch">字符串</param>
        /// <param name="stringArray">字符串数组</param>
        /// <param name="caseInsensetive">是否不区分大小写, true为不区分, false为区分</param>
        /// <returns>判断结果</returns>
        public static bool InArray(string strSearch, string[] stringArray, bool caseInsensetive)
        {
            return GetInArrayID(strSearch, stringArray, caseInsensetive) >= 0;
        }

        /// <summary>
        /// 判断指定字符串是否属于指定字符串数组中的一个元素
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="stringarray">字符串数组</param>
        /// <returns>判断结果</returns>
        public static bool InArray(string str, string[] stringarray)
        {
            return InArray(str, stringarray, false);
        }

        /// <summary>
        /// 判断指定字符串是否属于指定字符串数组中的一个元素
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="stringarray">内部以逗号分割单词的字符串</param>
        /// <returns>判断结果</returns>
        public static bool InArray(string str, string stringarray)
        {
            return InArray(str, SplitString(stringarray, ","), false);
        }

        /// <summary>
        /// 判断指定字符串是否属于指定字符串数组中的一个元素
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="stringarray">内部以逗号分割单词的字符串</param>
        /// <param name="strsplit">分割字符串</param>
        /// <returns>判断结果</returns>
        public static bool InArray(string str, string stringarray, string strsplit)
        {
            return InArray(str, SplitString(stringarray, strsplit), false);
        }

        /// <summary>
        /// 判断指定字符串是否属于指定字符串数组中的一个元素
        /// </summary>
        /// <param name="str">字符串</param>
        /// <param name="stringarray">内部以逗号分割单词的字符串</param>
        /// <param name="strsplit">分割字符串</param>
        /// <param name="caseInsensetive">是否不区分大小写, true为不区分, false为区分</param>
        /// <returns>判断结果</returns>
        public static bool InArray(string str, string stringarray, string strsplit, bool caseInsensetive)
        {
            return InArray(str, SplitString(stringarray, strsplit), caseInsensetive);
        }
        /// <summary>
        /// 判断文件名是否为浏览器可以直接显示的图片文件名
        /// </summary>
        /// <param name="filename">文件名</param>
        /// <returns>是否可以直接显示</returns>
        public static bool IsImgFilename(string filename)
        {
            filename = filename.Trim();
            if (filename.EndsWith(".") || filename.IndexOf(".") == -1)
            {
                return false;
            }
            string extname = filename.Substring(filename.LastIndexOf(".") + 1).ToLower();
            return (extname == "jpg" || extname == "jpeg" || extname == "png" || extname == "bmp" || extname == "gif");
        }
        /// <summary>
        /// 检测是否符合email格式
        /// </summary>
        /// <param name="strEmail">要判断的email字符串</param>
        /// <returns>判断结果</returns>
        public static bool IsValidEmail(string strEmail)
        {
            return Regex.IsMatch(strEmail, @"^([\w-\.]+)@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        public static bool IsValidDoEmail(string strEmail)
        {
            return Regex.IsMatch(strEmail, @"^@((\[[0-9]{1,3}\.[0-9]{1,3}\.[0-9]{1,3}\.)|(([\w-]+\.)+))([a-zA-Z]{2,4}|[0-9]{1,3})(\]?)$");
        }

        /// <summary>
        /// 是否为有效域
        /// </summary>
        /// <param name="host">域名</param>
        /// <returns></returns>
        public static bool IsValidDomain(string host)
        {
            Regex r = new Regex(@"^\d+$");
            if (host.IndexOf(".") == -1)
            {
                return false;
            }
            return r.IsMatch(host.Replace(".", string.Empty)) ? false : true;
        }

        /// <summary>
        /// 检测是否是正确的Url
        /// </summary>
        /// <param name="strUrl">要验证的Url</param>
        /// <returns>判断结果</returns>
        public static bool IsURL(string strUrl)
        {
            return Regex.IsMatch(strUrl, @"^(http|https)\://([a-zA-Z0-9\.\-]+(\:[a-zA-Z0-9\.&%\$\-]+)*@)*((25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9])\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[1-9]|0)\.(25[0-5]|2[0-4][0-9]|[0-1]{1}[0-9]{2}|[1-9]{1}[0-9]{1}|[0-9])|localhost|([a-zA-Z0-9\-]+\.)*[a-zA-Z0-9\-]+\.(com|edu|gov|int|mil|net|org|biz|arpa|info|name|pro|aero|coop|museum|[a-zA-Z]{1,10}))(\:[0-9]+)*(/($|[a-zA-Z0-9\.\,\?\'\\\+&%\$#\=~_\-]+))*$");
        }
        /// <summary>
        /// 判断是否为base64字符串
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsBase64String(string str)
        {
            //A-Z, a-z, 0-9, +, /, =
            return Regex.IsMatch(str, @"[A-Za-z0-9\+\/\=]");
        }
        /// <summary>
        /// 检测是否有Sql危险字符
        /// </summary>
        /// <param name="str">要判断字符串</param>
        /// <returns>判断结果</returns>
        public static bool IsSafeSqlString(string str)
        {

            return !Regex.IsMatch(str, @"[-|;|,|\/|\(|\)|\[|\]|\}|\{|%|@|\*|!|\']");
        }

        /// <summary>
        /// 检测是否有危险的可能用于链接的字符串
        /// </summary>
        /// <param name="str">要判断字符串</param>
        /// <returns>判断结果</returns>
        public static bool IsSafeUserInfoString(string str)
        {
            return !Regex.IsMatch(str, @"^\s*$|^c:\\con\\con$|[%,\*" + "\"" + @"\s\t\<\>\&]|游客|^Guest");
        }
        /// <summary>
        /// 验证是否为时间
        /// </summary>
        /// <returns></returns>
        public static bool IsTime(string timeval)
        {
            return Regex.IsMatch(timeval, @"^((([0-1]?[0-9])|(2[0-3])):([0-5]?[0-9])(:[0-5]?[0-9])?)$");
        }
        //0000 0000-0000 007F - 0xxxxxxx  (ascii converts to 1 octet!)
        //0000 0080-0000 07FF - 110xxxxx 10xxxxxx    ( 2 octet format)
        //0000 0800-0000 FFFF - 1110xxxx 10xxxxxx 10xxxxxx (3 octet format)

        /// <summary>
        /// 判断文件流是否为UTF8字符集
        /// </summary>
        /// <param name="sbInputStream">文件流</param>
        /// <returns>判断结果</returns>
        private static bool IsUTF8(FileStream sbInputStream)
        {
            int i;
            byte cOctets;  // octets to go in this UTF-8 encoded character 
            byte chr;
            bool bAllAscii = true;
            long iLen = sbInputStream.Length;

            cOctets = 0;
            for (i = 0; i < iLen; i++)
            {
                chr = (byte)sbInputStream.ReadByte();

                if ((chr & 0x80) != 0) bAllAscii = false;

                if (cOctets == 0)
                {
                    if (chr >= 0x80)
                    {
                        do
                        {
                            chr <<= 1;
                            cOctets++;
                        }
                        while ((chr & 0x80) != 0);

                        cOctets--;
                        if (cOctets == 0) return false;
                    }
                }
                else
                {
                    if ((chr & 0xC0) != 0x80)
                    {
                        return false;
                    }
                    cOctets--;
                }
            }

            if (cOctets > 0)
            {
                return false;
            }

            if (bAllAscii)
            {
                return false;
            }

            return true;

        }
        public static bool IsRuleTip(Hashtable NewHash, string ruletype, out string key)
        {
            key = "";
            foreach (DictionaryEntry str in NewHash)
            {
                try
                {
                    string[] single = SplitString(str.Value.ToString(), "\r\n");

                    foreach (string strs in single)
                    {
                        if (strs != "")


                            switch (ruletype.Trim().ToLower())
                            {
                                case "email":
                                    if (IsValidDoEmail(strs.ToString()) == false)
                                        throw new Exception();
                                    break;

                                case "ip":
                                    if (IsIPSect(strs.ToString()) == false)
                                        throw new Exception();
                                    break;

                                case "timesect":
                                    string[] splitetime = strs.Split('-');
                                    if (Utils.IsTime(splitetime[1].ToString()) == false || Utils.IsTime(splitetime[0].ToString()) == false)
                                        throw new Exception();
                                    break;

                            }
                    }
                }
                catch
                {
                    key = str.Key.ToString();
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 是否为ip
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static bool IsIP(string ip)
        {
            return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){3}(2[0-4]\d|25[0-5]|[01]?\d\d?)$");

        }

        public static bool IsIPSect(string ip)
        {
            return Regex.IsMatch(ip, @"^((2[0-4]\d|25[0-5]|[01]?\d\d?)\.){2}((2[0-4]\d|25[0-5]|[01]?\d\d?|\*)\.)(2[0-4]\d|25[0-5]|[01]?\d\d?|\*)$");

        }

        /// <summary>
        /// 返回指定IP是否在指定的IP数组所限定的范围内, IP数组内的IP地址可以使用*表示该IP段任意, 例如192.168.1.*
        /// </summary>
        /// <param name="ip"></param>
        /// <param name="iparray"></param>
        /// <returns></returns>
        public static bool InIPArray(string ip, string[] iparray)
        {

            string[] userip = Utils.SplitString(ip, @".");
            for (int ipIndex = 0; ipIndex < iparray.Length; ipIndex++)
            {
                string[] tmpip = Utils.SplitString(iparray[ipIndex], @".");
                int r = 0;
                for (int i = 0; i < tmpip.Length; i++)
                {
                    if (tmpip[i] == "*")
                    {
                        return true;
                    }

                    if (userip.Length > i)
                    {
                        if (tmpip[i] == userip[i])
                        {
                            r++;
                        }
                        else
                        {
                            break;
                        }
                    }
                    else
                    {
                        break;
                    }

                }
                if (r == 4)
                {
                    return true;
                }


            }
            return false;

        }
        /// <summary>
        /// 判断字符串是否是yy-mm-dd字符串
        /// </summary>
        /// <param name="str">待判断字符串</param>
        /// <returns>判断结果</returns>
        public static bool IsDateString(string str)
        {
            return Regex.IsMatch(str, @"(\d{4})-(\d{1,2})-(\d{1,2})");
        }
        /// <summary>
        /// 判断对象是否为Int32类型的数字
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static bool IsNumeric(object Expression)
        {
            return TypeParse.IsNumeric(Expression);
        }
        /// <summary>
        /// 验证时候为Double
        /// </summary>
        /// <param name="Expression"></param>
        /// <returns></returns>
        public static bool IsDouble(object Expression)
        {
            return TypeParse.IsDouble(Expression);
        }
        /// <summary>
        /// 判断给定的字符串数组(strNumber)中的数据是不是都为数值型
        /// </summary>
        /// <param name="strNumber">要确认的字符串数组</param>
        /// <returns>是则返加true 不是则返回 false</returns>
        public static bool IsNumericArray(string[] strNumber)
        {
            return TypeParse.IsNumericArray(strNumber);
        }
        /// <summary>
        /// 验证是否为正整数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsInt(string str)
        {
            return Regex.IsMatch(str, @"^[0-9]*$");
        }
        /// <summary>
        /// 字段串是否为Null或为""(空)
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool StrIsNullOrEmpty(string str)
        {
            //#if NET1
            if (str == null || str.Trim() == "")
            {
                return true;
            }
            //#else
            //            if (string.IsNullOrEmpty(str))
            //            {
            //                return true;
            //            }
            //#endif

            return false;
        }

        /// <summary>
        /// 是否为数值串列表，各数值间用","间隔
        /// </summary>
        /// <param name="numList"></param>
        /// <returns></returns>
        public static bool IsNumericList(string numList)
        {
            if (numList == "")
                return false;
            foreach (string num in numList.Split(','))
            {
                if (!IsNumeric(num))
                    return false;
            }
            return true;
        }

        /// <summary>
        /// 检查颜色值是否为3/6位的合法颜色
        /// </summary>
        /// <param name="color">待检查的颜色</param>
        /// <returns></returns>
        public static bool CheckColorValue(string color)
        {
            if (StrIsNullOrEmpty(color))
            {
                return false;
            }

            color = color.Trim().Trim('#');

            if (color.Length != 3 && color.Length != 6)
            {
                return false;
            }
            //不包含0-9  a-f以外的字符
            if (!Regex.IsMatch(color, "[^0-9a-f]", RegexOptions.IgnoreCase))
            {
                return true;
            }
            return false;
        }
        #endregion

        #region 分页
        /// <summary>
        /// 获得伪静态页码显示链接
        /// </summary>
        /// <param name="curPage">当前页数</param>
        /// <param name="countPage">总页数</param>
        /// <param name="url">超级链接地址</param>
        /// <param name="extendPage">周边页码显示个数上限</param>
        /// <returns>页码html</returns>
        public static string GetStaticPageNumbers(int curPage, int countPage, string url, string expname, int extendPage)
        {
            int startPage = 1;
            int endPage = 1;

            string t1 = "<a href=\"" + url + "-1" + expname + "\">&laquo;</a>";
            string t2 = "<a href=\"" + url + "-" + countPage + expname + "\">&raquo;</a>";

            if (countPage < 1) countPage = 1;
            if (extendPage < 3) extendPage = 2;

            if (countPage > extendPage)
            {
                if (curPage - (extendPage / 2) > 0)
                {
                    if (curPage + (extendPage / 2) < countPage)
                    {
                        startPage = curPage - (extendPage / 2);
                        endPage = startPage + extendPage - 1;
                    }
                    else
                    {
                        endPage = countPage;
                        startPage = endPage - extendPage + 1;
                        t2 = "";
                    }
                }
                else
                {
                    endPage = extendPage;
                    t1 = "";
                }
            }
            else
            {
                startPage = 1;
                endPage = countPage;
                t1 = "";
                t2 = "";
            }

            StringBuilder s = new StringBuilder("");

            s.Append(t1);
            for (int i = startPage; i <= endPage; i++)
            {
                if (i == curPage)
                {
                    s.Append("<span>");
                    s.Append(i);
                    s.Append("</span>");
                }
                else
                {
                    s.Append("<a href=\"");
                    s.Append(url);
                    s.Append("-");
                    s.Append(i);
                    s.Append(expname);
                    s.Append("\">");
                    s.Append(i);
                    s.Append("</a>");
                }
            }
            s.Append(t2);

            return s.ToString();
        }


        /// <summary>
        /// 获得伪静态页码显示链接
        /// </summary>
        /// <param name="expname"></param>
        /// <param name="countPage">总页数</param>
        /// <param name="url">超级链接地址</param>
        /// <param name="extendPage">周边页码显示个数上限</param>
        /// <returns>页码html</returns>
        public static string GetPostPageNumbers(int countPage, string url, string expname, int extendPage)
        {
            int startPage = 1;
            int endPage = 1;
            int curPage = 1;

            string t1 = "<a href=\"" + url + "-1" + expname + "\">&laquo;</a>";
            string t2 = "<a href=\"" + url + "-" + countPage + expname + "\">&raquo;</a>";

            if (countPage < 1) countPage = 1;
            if (extendPage < 3) extendPage = 2;

            if (countPage > extendPage)
            {
                if (curPage - (extendPage / 2) > 0)
                {
                    if (curPage + (extendPage / 2) < countPage)
                    {
                        startPage = curPage - (extendPage / 2);
                        endPage = startPage + extendPage - 1;
                    }
                    else
                    {
                        endPage = countPage;
                        startPage = endPage - extendPage + 1;
                        t2 = "";
                    }
                }
                else
                {
                    endPage = extendPage;
                    t1 = "";
                }
            }
            else
            {
                startPage = 1;
                endPage = countPage;
                t1 = "";
                t2 = "";
            }

            StringBuilder s = new StringBuilder("");

            s.Append(t1);
            for (int i = startPage; i <= endPage; i++)
            {
                s.Append("<a href=\"");
                s.Append(url);
                s.Append("-");
                s.Append(i);
                s.Append(expname);
                s.Append("\">");
                s.Append(i);
                s.Append("</a>");
            }
            s.Append(t2);

            return s.ToString();
        }



        /// <summary>
        /// 获得页码显示链接
        /// </summary>
        /// <param name="curPage">当前页数</param>
        /// <param name="countPage">总页数</param>
        /// <param name="url">超级链接地址</param>
        /// <param name="extendPage">周边页码显示个数上限</param>
        /// <returns>页码html</returns>
        public static string GetPageNumbers(int curPage, int countPage, string url, int extendPage)
        {
            return GetPageNumbers(curPage, countPage, url, extendPage, "page");
        }

        /// <summary>
        /// 获得页码显示链接
        /// </summary>
        /// <param name="curPage">当前页数</param>
        /// <param name="countPage">总页数</param>
        /// <param name="url">超级链接地址</param>
        /// <param name="extendPage">周边页码显示个数上限</param>
        /// <param name="pagetag">页码标记</param>
        /// <returns>页码html</returns>
        public static string GetPageNumbers(int curPage, int countPage, string url, int extendPage, string pagetag)
        {
            return GetPageNumbers(curPage, countPage, url, extendPage, pagetag, null);
        }

        /// <summary>
        /// 获得页码显示链接
        /// </summary>
        /// <param name="curPage">当前页数</param>
        /// <param name="countPage">总页数</param>
        /// <param name="url">超级链接地址</param>
        /// <param name="extendPage">周边页码显示个数上限</param>
        /// <param name="pagetag">页码标记</param>
        /// <param name="anchor">锚点</param>
        /// <returns>页码html</returns>
        public static string GetPageNumbers(int curPage, int countPage, string url, int extendPage, string pagetag, string anchor)
        {
            if (pagetag == "")
                pagetag = "page";
            int startPage = 1;
            int endPage = 1;

            if (url.IndexOf("?") > 0)
            {
                url = url + "&";
            }
            else
            {
                url = url + "?";
            }

            string t1 = "<a href=\"" + url + "&" + pagetag + "=1";
            string t2 = "<a href=\"" + url + "&" + pagetag + "=" + countPage;
            if (anchor != null)
            {
                t1 += anchor;
                t2 += anchor;
            }
            t1 += "\">&laquo;</a>";
            t2 += "\">&raquo;</a>";

            if (countPage < 1)
                countPage = 1;
            if (extendPage < 3)
                extendPage = 2;

            if (countPage > extendPage)
            {
                if (curPage - (extendPage / 2) > 0)
                {
                    if (curPage + (extendPage / 2) < countPage)
                    {
                        startPage = curPage - (extendPage / 2);
                        endPage = startPage + extendPage - 1;
                    }
                    else
                    {
                        endPage = countPage;
                        startPage = endPage - extendPage + 1;
                        t2 = "";
                    }
                }
                else
                {
                    endPage = extendPage;
                    t1 = "";
                }
            }
            else
            {
                startPage = 1;
                endPage = countPage;
                t1 = "";
                t2 = "";
            }

            StringBuilder s = new StringBuilder("");

            s.Append(t1);
            for (int i = startPage; i <= endPage; i++)
            {
                if (i == curPage)
                {
                    s.Append("<span>");
                    s.Append(i);
                    s.Append("</span>");
                }
                else
                {
                    s.Append("<a href=\"");
                    s.Append(url);
                    s.Append(pagetag);
                    s.Append("=");
                    s.Append(i);
                    if (anchor != null)
                    {
                        s.Append(anchor);
                    }
                    s.Append("\">");
                    s.Append(i);
                    s.Append("</a>");
                }
            }
            s.Append(t2);

            return s.ToString();
        }
        /// <summary>
        /// 获取ajax形式的分页链接
        /// </summary>
        /// <param name="curPage">当前页数</param>
        /// <param name="countPage">总页数</param>
        /// <param name="callback">回调函数</param>
        /// <param name="extendPage">周边页码显示个数上限</param>
        /// <returns></returns>
        public static string GetAjaxPageNumbers(int curPage, int countPage, string callback, int extendPage)
        {
            string pagetag = "page";
            int startPage = 1;
            int endPage = 1;

            string t1 = "<a href=\"###\" onclick=\"" + string.Format(callback, "&" + pagetag + "=1");
            string t2 = "<a href=\"###\" onclick=\"" + string.Format(callback, "&" + pagetag + "=" + countPage);

            t1 += "\">&laquo;</a>";
            t2 += "\">&raquo;</a>";

            if (countPage < 1)
                countPage = 1;
            if (extendPage < 3)
                extendPage = 2;

            if (countPage > extendPage)
            {
                if (curPage - (extendPage / 2) > 0)
                {
                    if (curPage + (extendPage / 2) < countPage)
                    {
                        startPage = curPage - (extendPage / 2);
                        endPage = startPage + extendPage - 1;
                    }
                    else
                    {
                        endPage = countPage;
                        startPage = endPage - extendPage + 1;
                        t2 = "";
                    }
                }
                else
                {
                    endPage = extendPage;
                    t1 = "";
                }
            }
            else
            {
                startPage = 1;
                endPage = countPage;
                t1 = "";
                t2 = "";
            }

            StringBuilder s = new StringBuilder("");

            s.Append(t1);
            for (int i = startPage; i <= endPage; i++)
            {
                if (i == curPage)
                {
                    s.Append("<span>");
                    s.Append(i);
                    s.Append("</span>");
                }
                else
                {
                    s.Append("<a href=\"###\" onclick=\"");
                    s.Append(string.Format(callback, pagetag + "=" + i));
                    s.Append("\">");
                    s.Append(i);
                    s.Append("</a>");
                }
            }
            s.Append(t2);

            return s.ToString();
        }
        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pagetotal"></param>
        /// <param name="total"></param>
        /// <returns></returns>
        public static string TenPage(int page, long pagetotal, long total)
        {
            long i_end = 1;
            string temp = "";


            int page_temp = page - 1;


            if (page_temp / 10 >= pagetotal / 10)
            {
                i_end = pagetotal;
            }
            else if (page_temp / 10 < pagetotal / 10)
            {
                i_end = page_temp / 10 * 10 + 10;
            }


            if ((page_temp / 10) >= 1)
            {
                temp = temp + "<a href=\"?page=" + (page_temp / 10 * 10) + "\" Titel=\"前十页\"><font style=\"font-family: Webdings;\" color=\"#ff9900\">9</font></a>&nbsp;&nbsp;";
            }

            for (int i = page_temp / 10 * 10 + 1; i <= i_end; i++)
            {
                if (i == page)
                {
                    temp = temp + "<font color=\"#ff6600\" >[" + i + "]</font>&nbsp;";
                }
                else
                {
                    temp = temp + "<a href=\"?page=" + i + "\" title=\"转到" + i + "页\" >[" + i + "]</a>&nbsp;";
                }

            }


            if (page < pagetotal)
            {
                if (page_temp / 10 < pagetotal / 10)
                {
                    temp = temp + "<a href=\"?page=" + ((page_temp / 10 + 1) * 10 + 1) + "\" Titel=\"后十页\"><font style=\"font-family: Webdings;\" color=\"#ff9900\">:</font></a>&nbsp;&nbsp;";
                }

            }
            temp = temp + "<input type=\"number\" name=\"go_page\" id=\"go_page\" value=\"" + page + "\"  /><input name=\"go_page_bt\" id=\"go_page_bt\" type=\"button\" value=\"GO\" onclick=\"javascript:void(location='?page='+$('#go_page').val()+'');\" />";
            return temp;
        }

        /// <summary>
        /// 分页
        /// </summary>
        /// <param name="page"></param>
        /// <param name="pagetotal"></param>
        /// <param name="total"></param>
        /// <param name="item"></param>
        /// <returns></returns>
        public static string TenPage(int page, long pagetotal, long total, string item)
        {
            long i_end = 1;
            string temp = null;
            int page_temp = page - 1;
            string temp_last = "<!--<a href=\"?page=" + (page - 1) + "" + item + "\"><img src=\"../images/sye.gif\" width=\"16\" height=\"15\" border =\"0\" /></a>&nbsp;&nbsp;-->";
            string temp_next = "<!--<a href=\"?page=" + (page + 1) + "" + item + "\"><img src=\"../images/xiaye.gif\" width=\"16\" height=\"15\" border =\"0\" /></a>&nbsp;&nbsp;-->";
            if (page_temp / 10 >= pagetotal / 10)
            {
                i_end = pagetotal;
            }
            else if (page_temp / 10 < pagetotal / 10)
            {
                i_end = page_temp / 10 * 10 + 10;
            }


            if ((page_temp / 10) >= 1)
            {
                temp = temp + "<a href=\"?page=" + (page_temp / 10 * 10) + "" + item + "\" Titel=\"前十页\"><font style=\"font-family: Webdings;\" color=\"#ff9900\">9</font></a>&nbsp;&nbsp;";

            }
            if (page > 1)
            {
                temp = temp + temp_last;
            }
            else
            {
                temp = temp + " <!--<img src=\"../images/sye.gif\" width=\"16\" height=\"15\" border =\"0\" />&nbsp;&nbsp;-->";
            }

            for (int i = page_temp / 10 * 10 + 1; i <= i_end; i++)
            {
                if (i == page)
                {
                    temp = temp + "<font color=\"#ff6600\" >[" + i + "]</font>&nbsp;";
                }
                else
                {
                    temp = temp + "<a href=\"?page=" + i + "" + item + "\" title=\"转到" + i + "页\" >[" + i + "]</a>&nbsp;";
                }

            }

            if (page < pagetotal)
            {
                temp = temp + temp_next;
            }
            else
            {
                temp = temp + " <!--<img src=\"../images/xiaye.gif\" width=\"16\" height=\"15\" border =\"0\" />&nbsp;&nbsp;-->";
            }

            if (page < pagetotal)
            {
                if (page_temp / 10 < pagetotal / 10)
                {
                    temp = temp + "<a href=\"?page=" + ((page_temp / 10 + 1) * 10 + 1) + "" + item + "\" Titel=\"后十页\"><font style=\"font-family: Webdings;\" color=\"#ff9900\">:</font></a>&nbsp;&nbsp;";
                }

            }

            temp = temp + "<input type=\"number\" name=\"go_page\" id=\"go_page\" value=\"" + page + "\"  /><input name=\"go_page_bt\" id=\"go_page_bt\" type=\"button\" value=\"GO\" onclick=\"javascript:void(location='?page='+$('#go_page').val()+'" + item + "');\" />";
            return temp;
        }
        #endregion

        #region 随机字符串
        /// <summary>
        /// 返回一个32位数的随机字符串
        /// </summary>
        public static string GetRanDomCode32()
        {
            string r_code = "";

            System.Random r = new Random(unchecked((int)DateTime.Now.Millisecond));
            r_code = Guid.NewGuid().ToString() + System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString() + System.DateTime.Now.Day.ToString() + System.DateTime.Now.Hour.ToString() + System.DateTime.Now.Minute.ToString() + System.DateTime.Now.Second.ToString() + System.DateTime.Now.Millisecond.ToString() + (8999 * r.Next(100) + 1000);
            r_code = MD5(r_code).ToLower();
            return r_code;
        }
        /// <summary>
        /// 返回一个16位数的随机字符串
        /// </summary>
        /// 
       

        public static string GetRanDomCode()
        {

            string r_code = "";
            r_code = GetRanDomCode32().Substring(8, 16).ToLower();
            return r_code;
        }

        /// <summary>
        /// 返回一个订单号随机字符串
        /// </summary>
        public static string GetRanOrderCode()
        {
            string r_code = "";

            System.Random r = new Random(unchecked((int)DateTime.Now.Millisecond));
            r_code = System.DateTime.Now.Year.ToString() + System.DateTime.Now.Month.ToString() + System.DateTime.Now.Day.ToString() + System.DateTime.Now.Hour.ToString() + System.DateTime.Now.Minute.ToString() + System.DateTime.Now.Second.ToString() + System.DateTime.Now.Millisecond.ToString() + (8999 * r.Next(100) + 1000);
            return r_code;
        }

        /// <summary>
        /// 返回一个订单号随机字符串
        /// </summary>
        /// <param name="PWDType">密码串类型,1=数字,2=字母,2=数字+字母</param>
        /// <param name="PWDLength">长度</param>
        public static string GetRanStr(int StrType, int StrLength)
        {
            char[] constant = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
            char[] constant2 = { 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };
            char[] constant3 = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'a', 'b', 'c', 'd', 'e', 'f', 'g', 'h', 'i', 'j', 'k', 'l', 'm', 'n', 'o', 'p', 'q', 'r', 's', 't', 'u', 'v', 'w', 'x', 'y', 'z' };

            string ReValue = "";
            System.Random r = new Random();
            for (int i = 0; i < StrLength; i++)
            {
                switch (StrType)
                {
                    case 1:
                        System.Text.StringBuilder sb = new System.Text.StringBuilder(10);
                        sb.Append(constant[r.Next(10)]);
                        ReValue = sb.ToString();
                        break;
                    case 2:
                        System.Text.StringBuilder sb2 = new System.Text.StringBuilder(26);
                        sb2.Append(constant2[r.Next(26)]);
                        ReValue = sb2.ToString();
                        break;
                    case 3:
                        System.Text.StringBuilder sb3 = new System.Text.StringBuilder(36);
                        sb3.Append(constant3[r.Next(36)]);
                        ReValue = sb3.ToString();
                        break;
                }
            }
            return ReValue;
        }
        #endregion

        #region 格式转换
        /// <summary>
        /// int型转换为string型
        /// </summary>
        /// <returns>转换后的string类型结果</returns>
        public static string IntToStr(int intValue)
        {
            //
            return Convert.ToString(intValue);
        }
        /// <summary>
        /// MD5函数
        /// </summary>
        /// <param name="str">原始字符串</param>
        /// <returns>MD5结果</returns>
        public static string MD5(string str)
        {
            byte[] b = Encoding.Default.GetBytes(str);
            b = new MD5CryptoServiceProvider().ComputeHash(b);
            string ret = "";
            for (int i = 0; i < b.Length; i++)
                ret += b[i].ToString("x").PadLeft(2, '0');
            return ret;
        }

        public static string GetMD5HashFromFile(string fileName)
        {
            try
            {
                FileStream file = new FileStream(fileName, FileMode.Open);
                System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                byte[] retVal = md5.ComputeHash(file);
                file.Close();

                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < retVal.Length; i++)
                {
                    sb.Append(retVal[i].ToString("x2"));
                }
                return sb.ToString();
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

        public static string GetMD5HashFromStream(Stream _Stream)
        {
            try
            {
                if (_Stream != null)
                {
                    System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();
                    byte[] retVal = md5.ComputeHash(_Stream);


                    StringBuilder sb = new StringBuilder();
                    for (int i = 0; i < retVal.Length; i++)
                    {
                        sb.Append(retVal[i].ToString("x2"));
                    }
                    return sb.ToString();
                }
                else {
                    return "";
                }
            }
            catch (Exception ex)
            {
                throw new Exception("GetMD5HashFromFile() fail,error:" + ex.Message);
            }
        }

        /// <summary>
        /// SHA256函数
        /// </summary>
        /// /// <param name="str">原始字符串</param>
        /// <returns>SHA256结果</returns>
        public static string SHA256(string str)
        {
            byte[] SHA256Data = Encoding.UTF8.GetBytes(str);
            SHA256Managed Sha256 = new SHA256Managed();
            byte[] Result = Sha256.ComputeHash(SHA256Data);
            return Convert.ToBase64String(Result);  //返回长度为44字节的字符串
        }
        /// <summary>
        /// 转换为简体中文
        /// </summary>
        public static string ToSChinese(string str)
        {
            return Strings.StrConv(str, VbStrConv.SimplifiedChinese, 0);
        }

        /// <summary>
        /// 转换为繁体中文
        /// </summary>
        public static string ToTChinese(string str)
        {
            return Strings.StrConv(str, VbStrConv.TraditionalChinese, 0);
        }
        /// <summary>
        /// 格式化字节数字符串
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static string FormatBytesStr(int bytes)
        {
            if (bytes > 1073741824)
            {
                return ((double)(bytes / 1073741824)).ToString("0") + "G";
            }
            if (bytes > 1048576)
            {
                return ((double)(bytes / 1048576)).ToString("0") + "M";
            }
            if (bytes > 1024)
            {
                return ((double)(bytes / 1024)).ToString("0") + "K";
            }
            return bytes.ToString() + "Bytes";
        }

        /// <summary>
        /// 将long型数值转换为Int32类型
        /// </summary>
        /// <param name="objNum"></param>
        /// <returns></returns>
        public static int SafeInt32(object objNum)
        {
            if (objNum == null)
            {
                return 0;
            }
            string strNum = objNum.ToString();
            if (IsNumeric(strNum))
            {

                if (strNum.ToString().Length > 9)
                {
                    if (strNum.StartsWith("-"))
                    {
                        return int.MinValue;
                    }
                    else
                    {
                        return int.MaxValue;
                    }
                }
                return Int32.Parse(strNum);
            }
            else
            {
                return 0;
            }
        }
        /// <summary>
        /// object型转换为bool型
        /// </summary>
        /// <param name="strValue">要转换的字符串</param>
        /// <param name="defValue">缺省值</param>
        /// <returns>转换后的bool类型结果</returns>
        public static bool StrToBool(object expression, bool defValue)
        {
            return TypeParse.StrToBool(expression, defValue);
        }

        /// <summary>
        /// string型转换为bool型
        /// </summary>
        /// <param name="strValue">要转换的字符串</param>
        /// <param name="defValue">缺省值</param>
        /// <returns>转换后的bool类型结果</returns>
        public static bool StrToBool(string expression, bool defValue)
        {
            return TypeParse.StrToBool(expression, defValue);
        }

        /// <summary>
        /// 将对象转换为Int32类型
        /// </summary>
        /// <param name="expression">要转换的字符串</param>
        /// <param name="defValue">缺省值</param>
        /// <returns>转换后的int类型结果</returns>
        public static int StrToInt(object expression, int defValue)
        {
            return TypeParse.StrToInt(expression, defValue);
        }

        /// <summary>
        /// 将字符串转换为Int32类型
        /// </summary>
        /// <param name="expression">要转换的字符串</param>
        /// <param name="defValue">缺省值</param>
        /// <returns>转换后的int类型结果</returns>
        public static int StrToInt(string expression, int defValue)
        {
            return TypeParse.StrToInt(expression, defValue);
        }

        /// <summary>
        /// Object型转换为float型
        /// </summary>
        /// <param name="strValue">要转换的字符串</param>
        /// <param name="defValue">缺省值</param>
        /// <returns>转换后的int类型结果</returns>
        public static float StrToFloat(object strValue, float defValue)
        {
            return TypeParse.StrToFloat(strValue, defValue);
        }

        /// <summary>
        /// string型转换为float型
        /// </summary>
        /// <param name="strValue">要转换的字符串</param>
        /// <param name="defValue">缺省值</param>
        /// <returns>转换后的int类型结果</returns>
        public static float StrToFloat(string strValue, float defValue)
        {
            return TypeParse.StrToFloat(strValue, defValue);
        }
        /// <summary>
        /// int转RGB
        /// </summary>
        public static Color IntToRGB(int color)
        {
            int r = 0xFF & color;
            int g = 0xFF00 & color;
            g >>= 8;
            int b = 0xFF0000 & color;
            b >>= 16;
            return Color.FromArgb(r, g, b);
        }
        /// <summary>
        /// RGB转16进制
        /// </summary>
        public static string RGBToStr(Color color)
        {
            return Utils.ReplaceString(System.Drawing.ColorTranslator.ToHtml(color).ToString(), "#", "", false);
        }

        private static string[] colorarr = { "ff0000", "ff3300", "ff6600", "ff9900", "ffcc00", "ffff00", "ccff00", "99ff00", "66ff00", "33ff00", "00ff00", "00ff33", "00ff66", "00ff99", "00ffcc", "00ffff", "00ccff", "0099ff", "0066ff", "0033ff", "0000ff", "3300ff", "6600ff", "9900ff", "cc00ff", "ff00ff", "ff00cc", "ff0099", "ff0066", "ff0033" };
        /// <summary>
        /// 数字转16进制颜色值
        /// </summary>
        public static string IntToColor(int colorindex)
        {
            if (colorindex < 0)
            {
                colorindex = 0;
            }
            if (colorindex >= colorarr.Length)
            {
                colorindex = colorarr.Length - 1;
            }
            return colorarr[colorindex];
        }
        /// <summary>
        /// 数字转16进制颜色值
        /// </summary>
        public static string IntToColor(int tValue, int AMin, int AMax)
        {
            float a = ValueBound(tValue, AMin, AMax, 0, colorarr.Length - 1);
            int colorindex = (int)a;
            return IntToColor(colorindex);
        }
        /// <summary>
        /// 将全角数字转换为数字
        /// </summary>
        /// <param name="SBCCase"></param>
        /// <returns></returns>
        public static string SBCCaseToNumberic(string SBCCase)
        {
            char[] c = SBCCase.ToCharArray();
            for (int i = 0; i < c.Length; i++)
            {
                byte[] b = System.Text.Encoding.Unicode.GetBytes(c, i, 1);
                if (b.Length == 2)
                {
                    if (b[1] == 255)
                    {
                        b[0] = (byte)(b[0] + 32);
                        b[1] = 0;
                        c[i] = System.Text.Encoding.Unicode.GetChars(b)[0];
                    }
                }
            }
            return new string(c);
        }

        /// <summary>
        /// 将字符串转换为Color
        /// </summary>
        /// <param name="color"></param>
        /// <returns></returns>
        public static Color ToColor(string color)
        {
            int red, green, blue = 0;
            char[] rgb;
            color = color.TrimStart('#');
            color = Regex.Replace(color.ToLower(), "[g-zG-Z]", "");
            switch (color.Length)
            {
                case 3:
                    rgb = color.ToCharArray();
                    red = Convert.ToInt32(rgb[0].ToString() + rgb[0].ToString(), 16);
                    green = Convert.ToInt32(rgb[1].ToString() + rgb[1].ToString(), 16);
                    blue = Convert.ToInt32(rgb[2].ToString() + rgb[2].ToString(), 16);
                    return Color.FromArgb(red, green, blue);
                case 6:
                    rgb = color.ToCharArray();
                    red = Convert.ToInt32(rgb[0].ToString() + rgb[1].ToString(), 16);
                    green = Convert.ToInt32(rgb[2].ToString() + rgb[3].ToString(), 16);
                    blue = Convert.ToInt32(rgb[4].ToString() + rgb[5].ToString(), 16);
                    return Color.FromArgb(red, green, blue);
                default:
                    return Color.FromName(color);

            }
        }
        /// <summary>
        /// 字符串转16进制
        /// </summary>
        public static string StrToHex(string strEncode)
        {
            string strReturn = "";//  存储转换后的编码
            foreach (short shortx in strEncode.ToCharArray())
            {
                strReturn += shortx.ToString("X");
            }
            return strReturn;
        }

        /// <summary>
        /// byte转string
        /// </summary>
        public static string ToHexString(byte[] bytes)
        {
            char[] hexDigits = { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9', 'A', 'B', 'C', 'D', 'E', 'F' };
            char[] chars = new char[bytes.Length * 2];
            for (int i = 0; i < bytes.Length; i++)
            {
                int b = bytes[i];
                chars[i * 2] = hexDigits[b >> 4];
                chars[i * 2 + 1] = hexDigits[b & 0xF];
            }
            return new string(chars);
        }

        /// <summary>
        ///作用：将16进制数据编码转化为字符串，是Encode的逆过程
        /// </summary>
        /// <param name="strDecode"></param>
        /// <returns></returns>
        public static string HexToStr(string strDecode)
        {
            string sResult = "";
            for (int i = 0; i < strDecode.Length / 2; i++)
            {
                sResult += (char)short.Parse(strDecode.Substring(i * 2, 2), global::System.Globalization.NumberStyles.HexNumber);
            }
            return sResult;
        }

        /// <summary>
        /// 将16进制数据编码转化为字节
        /// </summary>
        public static byte[] HexStrToBytes(string HexStr)
        {
            byte[] reAsmCode = new byte[HexStr.Length / 2];
            for (int i = 0; i < reAsmCode.Length; i++)
            {
                reAsmCode[i] = Convert.ToByte(Int32.Parse(HexStr.Substring(i * 2, 2), System.Globalization.NumberStyles.AllowHexSpecifier));
            }
            return reAsmCode;
        }

        /// <summary>
        /// 将Unicode字串\u....\u....格式字串转换为原始字符串
        /// </summary>
        /// <param name="srcText"></param>
        /// <returns></returns>
        public static string UnicodeToString(string srcText)
        {
            string dst = "";
            string src = srcText;
            int len = srcText.Length / 6;

            for (int i = 0; i <= len - 1; i++)
            {
                string str = "";
                str = src.Substring(0, 6).Substring(2);
                src = src.Substring(6);
                byte[] bytes = new byte[2];
                bytes[1] = byte.Parse(int.Parse(str.Substring(0, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                bytes[0] = byte.Parse(int.Parse(str.Substring(2, 2), System.Globalization.NumberStyles.HexNumber).ToString());
                dst += Encoding.Unicode.GetString(bytes);
            }
            return dst;
        }
        /// <summary>
        /// 字符串转换成Unicode字串\u....\u....格式字串
        /// </summary>
        /// <param name="arg_Source"></param>
        /// <returns></returns>
        public static string StringToUnicode(string arg_Source)
        {
            int enCode;
            char[] charArray = arg_Source.ToCharArray();
            StringBuilder uniText = new StringBuilder(arg_Source.Length * 2);
            for (int i = 0; i < charArray.Length; i++)
            {
                char a = charArray[i];
                uniText.Append("\\u");
                enCode = (a >> 8);
                string hexCode = enCode.ToString("X");
                if (hexCode.ToString().Length == 1)
                {
                    uniText.Append("0");
                }
                uniText.Append(hexCode);
                enCode = (a & 0xFF);
                hexCode = enCode.ToString("X");
                if (hexCode.ToString().Length == 1)
                {
                    uniText.Append("0");
                }
                uniText.Append(hexCode);
            }
            return (uniText.ToString());
        }
        /// <summary>
        /// 进制转换
        /// </summary>
        public static string ConvertString(string value, int fromBase, int toBase)
        {

            int intValue = Convert.ToInt32(value, fromBase);

            return Convert.ToString(intValue, toBase);
        }
        /// <summary>
        /// 反转字符串
        /// </summary>
        public static string Reverse(string original)
        {
            char[] arr = original.ToCharArray();
            Array.Reverse(arr);
            return new string(arr);
        }
        #endregion

        #region 任意进制转换
        /// <summary>
        /// 任意进制转换
        /// </summary>
        /// <param name="baseChars">表示从0-N的字符序列</param>
        public static string BaseConverterToString(string baseChars, int value)
        {
            BaseConverter bc = new BaseConverter(baseChars.ToCharArray());
            return bc.ToString(value);
        }
        public class BaseConverter
        {
            protected List<char> _chars = new List<char>();
            protected Dictionary<char, int> _charmap = new Dictionary<char, int>();
            protected List<long> _preBitValue = new List<long>();

            /**/
            /// <summary>
            /// 得到进制指定权位的值
            /// </summary>
            /// <param name="pos">权位</param>
            /// <returns>权位的数值</returns>
            protected long GetPowerValue(int pos)
            {
                if (_preBitValue.Count < pos)
                {
                    for (int i = _preBitValue.Count; i <= pos; ++i)
                    {
                        _preBitValue.Add(Convert.ToInt64(Math.Pow(_chars.Count, i)));
                    }
                }

                return _preBitValue[pos];
            }

            /**/
            /// <summary>
            /// 构造一个指定进制和字符的转换器
            /// </summary>
            /// <param name="baseChars">表示从0-N的字符序列</param>
            public BaseConverter(char[] baseChars)
            {
                _chars.AddRange(baseChars);

                for (int i = 0; i < baseChars.Length; ++i)
                {
                    _charmap.Add(baseChars[i], i);
                }
            }

            /**/
            /// <summary>
            /// 把用指定进制和字符的字串, 解释成等值的十进制数值
            /// </summary>
            /// <param name="value">指定进制和字符的字串</param>
            /// <returns>等值的十进制数值</returns>
            public long ToNumber(string value)
            {
                char[] chars = value.ToCharArray();

                long ret = 0;
                for (int i = 0; i < chars.Length; ++i)
                {
                    ret += GetPowerValue(chars.Length - 1 - i) * _charmap[chars[i]];
                }

                return ret;
            }

            /**/
            /// <summary>
            /// 把当前十进制数值用指定的进制和字符表现出来
            /// </summary>
            /// <param name="value">十进制数值</param>
            /// <returns>表现出来的字串</returns>
            public string ToString(long value)
            {
                int power = _chars.Count;
                List<char> list = new List<char>();

                while (value > 0)
                {
                    int l = Convert.ToInt32(value % power);
                    value /= power;

                    list.Add(_chars[l]);
                }

                list.Reverse();

                return new string(list.ToArray());
            }
        }
        #endregion

        #region XML 处理
        /// <summary>
        /// 返回XML节点数据
        /// </summary>
        public static string GetXMLData(System.Xml.XmlDocument xmlDoc, string name)
        {
            string result = "";
            if (name != null && name != "")
            {
                System.Xml.XmlNodeList node = xmlDoc.GetElementsByTagName(name);
                if (node != null && node.Count > 0)
                {
                    result = System.Web.HttpUtility.UrlDecode(node[0].FirstChild.Value);
                }
            }
            return result;
        }

        /// <summary>
        /// 返回指定XML节点中间子节点数据
        /// </summary>
        public static string GetXMLNodeListData(System.Xml.XmlNodeList node, string name)
        {
            string result = "";
            if (name != null && name != "")
            {
                if (node != null && node.Count > 0)
                {
                    for (int i = 0; i < node.Count; i++)
                    {
                        if (node[i].Name == name)
                        {
                            try
                            {
                                result = System.Web.HttpUtility.UrlDecode(node[i].FirstChild.Value);
                                break;
                            }
                            catch
                            {
                                result = "";
                                break;
                            }
                        }
                    }
                }
            }
            return result;
        }
        #endregion

        #region Excel操作
        /// <summary>
        /// 读取Excel
        /// </summary>
        /// <param name="strExcelFileName"></param>
        /// <param name="strSheetName"></param>
        /// <returns></returns>
        public static DataTable ExcelToDataTable(string strExcelFileName, string strSheetName)
        {
            //源的定义
            string strConn = "Provider=Microsoft.Jet.OLEDB.4.0;" + "Data Source=" + strExcelFileName + ";" + "Extended Properties='Excel 8.0;HDR=NO;IMEX=1';";

            //Sql语句
            //string strExcel = string.Format("select * from [{0}$]", strSheetName); 这是一种方法
            string strExcel = "select * from [sheet1$]";

            //定义存放的数据表
            DataTable dt = new DataTable();
            DataSet ds = new DataSet();

            //连接数据源
            OleDbConnection conn = new OleDbConnection(strConn);
            try
            {
                try
                {
                    conn.Open();
                    //适配到数据源
                    OleDbDataAdapter adapter = new OleDbDataAdapter(strExcel, strConn);
                    try
                    {
                        adapter.Fill(ds, strSheetName);
                    }
                    finally
                    {
                        adapter.Dispose();
                    }
                    dt = ds.Tables[strSheetName];
                }
                catch//使用XML方式读取
                {
                    ds.ReadXml(strExcelFileName);

                    dt = ds.Tables[strSheetName];
                }
            }
            finally
            {
                conn.Close();
            }

            return dt;
        }
        #endregion

        #region 客户端类型
        public static bool IsMoblie()
        {
            string agent = (HTTPRequest.UserAgent() + "").ToLower().Trim();

            if (agent == "" ||
                agent.IndexOf("mobile") != -1 ||
                agent.IndexOf("mobi") != -1 ||
                agent.IndexOf("nokia") != -1 ||
                agent.IndexOf("samsung") != -1 ||
                agent.IndexOf("sonyericsson") != -1 ||
                agent.IndexOf("mot") != -1 ||
                agent.IndexOf("blackberry") != -1 ||
                agent.IndexOf("lg") != -1 ||
                agent.IndexOf("htc") != -1 ||
                agent.IndexOf("j2me") != -1 ||
                agent.IndexOf("ucweb") != -1 ||
                agent.IndexOf("opera mini") != -1 ||
                agent.IndexOf("mobi") != -1 ||
                agent.IndexOf("android") != -1 ||
                agent.IndexOf("iphone") != -1)
            {
                //终端可能是手机

                return true;

            }

            return false;
        }
        #endregion
    }

    public class LockBitmap
    {
        Bitmap source = null;
        IntPtr Iptr = IntPtr.Zero;
        BitmapData bitmapData = null;

        public byte[] Pixels { get; set; }
        public int Depth { get; private set; }
        public int Width { get; private set; }
        public int Height { get; private set; }

        public LockBitmap(Bitmap source)
        {
            this.source = source;
        }

        /// <summary>
        /// Lock bitmap data
        /// </summary>
        public void LockBits()
        {
            try
            {
                // Get width and height of bitmap
                Width = source.Width;
                Height = source.Height;

                // get total locked pixels count
                int PixelCount = Width * Height;

                // Create rectangle to lock
                Rectangle rect = new Rectangle(0, 0, Width, Height);

                // get source bitmap pixel format size
                Depth = System.Drawing.Bitmap.GetPixelFormatSize(source.PixelFormat);

                // Check if bpp (Bits Per Pixel) is 8, 24, or 32
                if (Depth != 8 && Depth != 24 && Depth != 32)
                {
                    throw new ArgumentException("Only 8, 24 and 32 bpp images are supported.");
                }

                // Lock bitmap and return bitmap data
                bitmapData = source.LockBits(rect, ImageLockMode.ReadWrite,
                                             source.PixelFormat);

                // create byte array to copy pixel values
                int step = Depth / 8;
                Pixels = new byte[PixelCount * step];
                Iptr = bitmapData.Scan0;

                // Copy data from pointer to array
                Marshal.Copy(Iptr, Pixels, 0, Pixels.Length);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Unlock bitmap data
        /// </summary>
        public void UnlockBits()
        {
            try
            {
                // Copy data from byte array to pointer
                Marshal.Copy(Pixels, 0, Iptr, Pixels.Length);

                // Unlock bitmap data
                source.UnlockBits(bitmapData);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <returns></returns>
        public Color GetPixel(int x, int y)
        {
            Color clr = Color.Empty;

            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (i > Pixels.Length - cCount)
                throw new IndexOutOfRangeException();

            if (Depth == 32) // For 32 bpp get Red, Green, Blue and Alpha
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                byte a = Pixels[i + 3]; // a
                clr = Color.FromArgb(a, r, g, b);
            }
            if (Depth == 24) // For 24 bpp get Red, Green and Blue
            {
                byte b = Pixels[i];
                byte g = Pixels[i + 1];
                byte r = Pixels[i + 2];
                clr = Color.FromArgb(r, g, b);
            }
            if (Depth == 8)
            // For 8 bpp get color value (Red, Green and Blue values are the same)
            {
                byte c = Pixels[i];
                clr = Color.FromArgb(c, c, c);
            }
            return clr;
        }

        /// <summary>
        /// Set the color of the specified pixel
        /// </summary>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="color"></param>
        public void SetPixel(int x, int y, Color color)
        {
            // Get color components count
            int cCount = Depth / 8;

            // Get start index of the specified pixel
            int i = ((y * Width) + x) * cCount;

            if (Depth == 32) // For 32 bpp set Red, Green, Blue and Alpha
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
                Pixels[i + 3] = color.A;
            }
            if (Depth == 24) // For 24 bpp set Red, Green and Blue
            {
                Pixels[i] = color.B;
                Pixels[i + 1] = color.G;
                Pixels[i + 2] = color.R;
            }
            if (Depth == 8)
            // For 8 bpp set color value (Red, Green and Blue values are the same)
            {
                Pixels[i] = color.B;
            }
        }
    }
}
