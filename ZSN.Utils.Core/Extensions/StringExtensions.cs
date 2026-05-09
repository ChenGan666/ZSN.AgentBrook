using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using JiebaNet.Segmenter.Common;
using Microsoft.Extensions.Primitives;
using Newtonsoft.Json;

namespace ZSN.Utils.Core.Extensions
{
    /// <summary>
    /// 字符串扩展方法
    /// </summary>
    public static class StringExtensions
    {
        #region 字符串操作

        /// <summary>
        /// 获取字符串的实际长度(按单字节)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int GetRealLength(this string source)
        {
            return Encoding.Default.GetByteCount(source);
        }

        /// <summary>
        /// 取得固定长度的字符串(按单字节截取)。
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="resultLength">截取长度</param>
        /// <returns></returns>
        public static string SubString(this string source, int resultLength)
        {
            //判断字符串长度是否大于截断长度
            if (Encoding.Default.GetByteCount(source) > resultLength)
            {
                //判断字串是否为空
                if (source == null)
                {
                    return "";
                }

                //初始化
                int i = 0, j = 0;

                //为汉字或全脚符号长度加2否则加1
                foreach (char newChar in source)
                {
                    if (newChar > 127)
                    {
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                    if (i > resultLength)
                    {
                        source = source.Substring(0, j);
                        break;
                    }
                    j++;
                }
            }
            return source;
        }

        /// <summary>
        /// 取得固定长度字符的字符串，后面加上…
        /// </summary>
        /// <param name="source"></param>
        /// <param name="resultLength"></param>
        /// <returns></returns>
        public static string SubStr(this string source, int resultLength)
        {
            //判断字串是否为空
            if (string.IsNullOrEmpty(source))
            {
                return "";
            }

            if (source.GetRealLength() <= resultLength)
            {
                return source;
            }
            else
            {
                return source.SubString(resultLength) + "...";
            }
        }
        /// <summary>
        /// 去除字符串的html标签
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string RemoveHtmlTags(this string source)
        {
            return Regex.Replace(source, @"<[^>]+>", "", RegexOptions.IgnoreCase);
        }

        public static string ToTitle(this string source)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(source.ToLower());
        }

        /// <summary>
        /// 首字母小写
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToInitialLower(this string source)
        {
            return source.Substring(0, 1).ToLower() + source.Substring(1);
        }

        #endregion

        #region 字符串格式验证

        /// <summary>
        /// 判断字符串是否为null或为空.判断为空操作前先进行了Trim操作。
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsNullOrEmpty(this string source)
        {
            if (source != null)
            {
                return source.Trim().Length < 1;
            }
            return true;
        }

        /// <summary>
        /// 判断字符串是否为整型
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsInteger(this string source)
        {
            return !source.IsNullOrEmpty() && int.TryParse(source, out _);
        }

        /// <summary>
        /// Email 格式是否合法
        /// </summary>
        /// <param name="source"></param>
        public static bool IsEmail(this string source)
        {
            return Regex.IsMatch(source, @"^\w+((-\w+)|(\.\w+))*\@[A-Za-z0-9]+((\.|-)[A-Za-z0-9]+)*\.[A-Za-z0-9]+$");
        }

        /// <summary>
        /// 判断是否IP
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsIP(this string source)
        {
            return Regex.IsMatch(source, @"^(((25[0-5]|2[0-4][0-9]|19[0-1]|19[3-9]|18[0-9]|17[0-1]|17[3-9]|1[0-6][0-9]|1[1-9]|[2-9][0-9]|[0-9])\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9]))|(192\.(25[0-5]|2[0-4][0-9]|16[0-7]|169|1[0-5][0-9]|1[7-9][0-9]|[1-9][0-9]|[0-9]))|(172\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|1[0-5]|3[2-9]|[4-9][0-9]|[0-9])))\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9])\.(25[0-5]|2[0-4][0-9]|1[0-9][0-9]|[1-9][0-9]|[0-9])$");
        }

        /// <summary>
        /// 检查字符串是否为A-Z、0-9及下划线以内的字符
        /// </summary>
        /// <param name="source">被检查的字符串</param>
        /// <returns>是否有特殊字符</returns>
        public static bool IsLetterOrNumber(this string source)
        {
            bool b = Regex.IsMatch(source, @"\w");
            return b;
        }

        /// <summary>
        /// 验输入字符串是否含有“/\:.?*|$]”特殊字符
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsSpecialChar(this string source)
        {
            Regex r = new Regex(@"[/\<>:.?*|$]");
            return r.IsMatch(source);
        }

        /// <summary>
        /// 是否全为中文/日文/韩文字符
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static bool IsChineseChar(this string source)
        {
            //中文/日文/韩文: [\u4E00-\u9FA5]
            //英文:[a-zA-Z]
            return Regex.IsMatch(source, @"^[\u4E00-\u9FA5]+$");
        }

        /// <summary>
        /// 判断字符串中是否包含中文
        /// </summary>
        /// <param name="str">需要判断的字符串</param>
        /// <returns>判断结果</returns>
        public static bool HasChinese(this string str)
        {
            return Regex.IsMatch(str, @"[\u4e00-\u9fa5]");
        }

        /// <summary>
        /// 是否包含双字节字符(允许有单字节字符)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsDoubleChar(this string source)
        {
            return Regex.IsMatch(source, @"[^\x00-\xff]");
        }

        /// <summary>
        /// 是否为日期型字符串
        /// </summary>
        /// <param name="source">日期字符串(2005-6-30)</param>
        /// <returns></returns>
        public static bool IsDate(this string source)
        {
            return Regex.IsMatch(source, @"^((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-))$");
        }

        /// <summary>
        /// 是否为时间型字符串
        /// </summary>
        /// <param name="source">时间字符串(15:00:00)</param>
        /// <returns></returns>
        public static bool IsTime(this string source)
        {
            return Regex.IsMatch(source, @"^((20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d)$");
        }

        /// <summary>
        /// 是否为日期+时间型字符串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsDateTime(this string source)
        {
            return !string.IsNullOrWhiteSpace(source) && Regex.IsMatch(source, @"^(((((1[6-9]|[2-9]\d)\d{2})-(0?[13578]|1[02])-(0?[1-9]|[12]\d|3[01]))|(((1[6-9]|[2-9]\d)\d{2})-(0?[13456789]|1[012])-(0?[1-9]|[12]\d|30))|(((1[6-9]|[2-9]\d)\d{2})-0?2-(0?[1-9]|1\d|2[0-8]))|(((1[6-9]|[2-9]\d)(0[48]|[2468][048]|[13579][26])|((16|[2468][048]|[3579][26])00))-0?2-29-)) (20|21|22|23|[0-1]?\d):[0-5]?\d:[0-5]?\d)$");
        }

        /// <summary>
        /// 是否是手机号码
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static bool IsMobile(this string source)
        {
            return Regex.IsMatch(source, @"^[1]+[3,5,7,8]+\d{9}$");
        }

        #endregion

        #region 字符串编码

        /// <summary>
        /// 将字符串使用base64算法加密
        /// </summary>
        /// <param name="source">待加密的字符串</param>
        /// <returns>加码后的文本字符串</returns>
        public static string ToBase64(this string source)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(source));
        }

        public static string ToBase64(this string source, Encoding encodeType)
        {
            byte[] bytes = encodeType.GetBytes(source);
            string encode;
            try
            {
                encode = Convert.ToBase64String(bytes);
            }
            catch
            {
                encode = source;
            }
            return encode;
        }

        /// <summary>
        /// Base64Url安全编码
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToBase64SafeUrl(this string source)
        {
            return Convert.ToBase64String(Encoding.UTF8.GetBytes(source)).Replace("+", "-").Replace("/", "_");
        }

        /// <summary>
        /// Base64Url安全编码
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToBase64SafeUrl(this byte[] source)
        {
            return Convert.ToBase64String(source).Replace("+", "-").Replace("/", "_");
        }

        /// <summary>
        /// 从Base64编码的字符串中还原字符串，支持中文
        /// </summary>
        /// <param name="source">Base64加密后的字符串</param>
        /// <returns>还原后的文本字符串</returns>
        public static string FromBase64(this string source)
        {
            return Encoding.UTF8.GetString(Convert.FromBase64String(source));
        }

        public static string FromBase64(this string source, Encoding encodeType)
        {
            byte[] bytes = Convert.FromBase64String(source);
            string decode;
            try
            {
                decode = encodeType.GetString(bytes);
            }
            catch
            {
                decode = source;
            }
            return decode;
        }

        /// <summary>
        /// 将 GB2312 值转换为 UTF8 字符串(如：测试 -> 娴嬭瘯 )
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string FromGBToUTF8(this string source)
        {
            return Encoding.GetEncoding("GB2312").GetString(Encoding.UTF8.GetBytes(source));
        }

        /// <summary>
        /// 将 UTF8 值转换为 GB2312 字符串 (如：娴嬭瘯 -> 测试)
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string FromUTF8ToGB(this string source)
        {
            return Encoding.UTF8.GetString(Encoding.GetEncoding("GB2312").GetBytes(source));
        }

        /// <summary>
        /// 由16进制转为汉字字符串（如：B2E2 -> 测 ）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string FromHex(this string source)
        {
            byte[] oribyte = new byte[source.Length / 2];
            for (int i = 0; i < source.Length; i += 2)
            {
                string str = Convert.ToInt32(source.Substring(i, 2), 16).ToString();
                oribyte[i / 2] = Convert.ToByte(source.Substring(i, 2), 16);
            }
            return Encoding.Default.GetString(oribyte);
        }

        /// <summary>
        /// 字符串转为16进制字符串（如：测 -> B2E2 ）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToHex(this string source)
        {
            int i = source.Length;
            string temp;
            string end = "";
            byte[] array = new byte[2];
            int i1, i2;
            for (int j = 0; j < i; j++)
            {
                temp = source.Substring(j, 1);
                array = Encoding.Default.GetBytes(temp);
                if (array.Length.ToString() == "1")
                {
                    i1 = Convert.ToInt32(array[0]);
                    end += Convert.ToString(i1, 16);
                }
                else
                {
                    i1 = Convert.ToInt32(array[0]);
                    i2 = Convert.ToInt32(array[1]);
                    end += Convert.ToString(i1, 16);
                    end += Convert.ToString(i2, 16);
                }
            }
            return end.ToUpper();
        }

        /// <summary>
        /// 字符串转为unicode字符串（如：测试 -> &#27979;&#35797;）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToUnicode(this string source)
        {
            StringBuilder sa = new StringBuilder();//Unicode
            string s1;
            string s2;
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bt = Encoding.Unicode.GetBytes(source.Substring(i, 1));
                if (bt.Length > 1)//判断是否汉字
                {
                    s1 = Convert.ToString((short)(bt[1] - '\0'), 16);//转化为16进制字符串
                    s2 = Convert.ToString((short)(bt[0] - '\0'), 16);//转化为16进制字符串
                    s1 = (s1.Length == 1 ? "0" : "") + s1;//不足位补0
                    s2 = (s2.Length == 1 ? "0" : "") + s2;//不足位补0
                    sa.Append("&#" + Convert.ToInt32(s1 + s2, 16) + ";");
                }
            }

            return sa.ToString();
        }

        /// <summary>
        /// 字符串转为UTF8字符串（如：测试 -> \u6d4b\u8bd5）
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToUTF8(this string source)
        {
            StringBuilder sb = new StringBuilder();//UTF8
            string s1;
            string s2;
            for (int i = 0; i < source.Length; i++)
            {
                byte[] bt = Encoding.Unicode.GetBytes(source.Substring(i, 1));
                if (bt.Length > 1)//判断是否汉字
                {
                    s1 = Convert.ToString((short)(bt[1] - '\0'), 16);//转化为16进制字符串
                    s2 = Convert.ToString((short)(bt[0] - '\0'), 16);//转化为16进制字符串
                    s1 = (s1.Length == 1 ? "0" : "") + s1;//不足位补0
                    s2 = (s2.Length == 1 ? "0" : "") + s2;//不足位补0
                    sb.Append("\\u" + s1 + s2);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// 将字符串转为安全的Sql字符串，不建议使用。尽可能使用参数化查询来避免
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToSafeSql(this string source)
        {
            if (source == null)
                return String.Empty;

            source = source.Trim();
            //删除脚本、注入相关字符
            source = Regex.Replace(source, @"<script[^>]*?>.*?</script>", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"-->", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"<!--.*", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"select", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"drop", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"update", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"insert", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"delete", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"'", "", RegexOptions.IgnoreCase);
            source = Regex.Replace(source, @"sleep", "", RegexOptions.IgnoreCase);
            return source;
        }

        /// <summary>
        /// 将字符串转换化安全的js字符串值（对字符串中的' "进行转义) 
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToSafeJsString(this string source)
        {
            source = source.Replace("'", "\\'");
            source = source.Replace("\"", "\\\"");
            source = source.Replace("\r\n", "");
            return source;
        }

        /// <summary>
        /// 将字符串包装成 <![CDATA[字符串]]> 形式
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string WrapWithCData(this string source)
        {
            return string.Format("<![CDATA[{0}]]>", source);
        }

        /// <summary>
        /// 将字符串转成SEO优化的字符串
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToSEOUrl(this string source)
        {
            return Regex.Replace(source, @"[^a-zA-Z0-9]", "-", RegexOptions.IgnoreCase);
        }
        #endregion

        #region 类型转换

        public static string ToStringOrEnpty(this string source)
        {
            return source ?? string.Empty;
        }

        /// <summary>
        /// 将字符串转成Int16类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static short ToInt16(this string source, short defaultValue = -1)
        {
            if (!string.IsNullOrEmpty(source))
            {
                if (short.TryParse(source, out var result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成Int32类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static int ToInt32(this string source, int defaultValue = -1)
        {
            if (!string.IsNullOrEmpty(source))
            {
                int result;
                if (int.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成Int64类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static long ToInt64(this string source, long defaultValue = -1)
        {
            if (!string.IsNullOrEmpty(source))
            {
                long result;
                if (long.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成double类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static double ToDouble(this string source, double defaultValue = -1)
        {
            if (!string.IsNullOrEmpty(source))
            {
                double result;
                if (double.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成Decimal类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的数值</param>
        /// <returns></returns>
        public static decimal ToDecimal(this string source, decimal defaultValue = -1)
        {
            if (!string.IsNullOrEmpty(source))
            {
                decimal result;
                if (decimal.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成DateTime类型，如果转换失败，则返回当前时间
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string source)
        {
            return source.ToDateTime(DateTime.Now);
        }

        /// <summary>
        /// 将字符串转成DateTime类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回的默认时间</param>
        /// <returns></returns>
        public static DateTime ToDateTime(this string source, DateTime defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                DateTime result;
                if (DateTime.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成Boolean类型
        /// </summary>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败返回的默认值</param>
        /// <returns></returns>
        public static bool ToBoolean(this string source, bool defaultValue = false)
        {
            if (!string.IsNullOrEmpty(source))
            {
                bool result;
                if (bool.TryParse(source, out result))
                {
                    return result;
                }
            }
            return defaultValue;
        }

        /// <summary>
        /// 将字符串转成指定的枚举类型(字符串可以是枚举的名称也可以是枚举值)
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="source">源字符串</param>
        /// <param name="defaultValue">如果转换失败，返回默认的枚举项</param>
        /// <returns></returns>
        public static T ToEnum<T>(this string source, T defaultValue)
        {
            if (!string.IsNullOrEmpty(source))
            {
                try
                {
                    T value = (T)Enum.Parse(typeof(T), source, true);
                    if (Enum.IsDefined(typeof(T), value))
                    {
                        return value;
                    }
                }
                catch { }
            }
            return defaultValue;
        }

        #endregion

        /// <summary>
        /// 获取缩略图路径
        /// </summary>
        /// <param name="source"></param>
        /// <param name="size">112x220</param>
        /// <returns></returns>
        public static string GetThumbnailPath(this string source, string size)
        {
            if (!string.IsNullOrEmpty(source))
            {
                string ext = Path.GetExtension(source);
                return source.Replace(ext, "_" + size + ext);
            }
            return "";
        }

        /// <summary>
        /// 获取哈希码
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int ToHashCode(this string source)
        {
            unchecked
            {
                int hash = 23;
                foreach (char c in source)
                {
                    hash = hash * 31 + c;
                }
                return hash;
            }
        }

        public static long ToHashCode64(this string source)
        {
            unchecked
            {
                long hash = 23;
                foreach (char c in source)
                {
                    hash = hash * 61 + c;
                }
                return hash;
            }
        }

        public static int ToHashSign(this string source)
        {
            int myCode = Convert.ToInt32((Math.Abs(source.ToHashCode()) % 1000000).ToString().PadLeft(6, '1'));
            return myCode;
        }

        /// <summary>
        /// 十六进制字符转字节
        /// </summary>
        /// <param name="sourceChar">十六进制字符(0-9,A-F)</param>
        /// <returns>字节</returns>
        public static byte HexCharToByte(string sourceChar)
        {
            if (sourceChar.Length > 1)
                sourceChar = sourceChar.Substring(0, 1);

            switch (sourceChar.ToUpper())
            {
                case "0":
                    return 0x00;
                case "1":
                    return 0x01;
                case "2":
                    return 0x02;
                case "3":
                    return 0x03;
                case "4":
                    return 0x04;
                case "5":
                    return 0x05;
                case "6":
                    return 0x06;
                case "7":
                    return 0x07;
                case "8":
                    return 0x08;
                case "9":
                    return 0x09;
                case "A":
                    return 0x0a;
                case "B":
                    return 0x0b;
                case "C":
                    return 0x0c;
                case "D":
                    return 0x0d;
                case "E":
                    return 0x0e;
                case "F":
                    return 0x0f;
                default:
                    return 0x00;
            }
        }

        /// <summary>
        /// 十六进制字符串转字节数组
        /// </summary>
        /// <param name="sourceStr">十六进制字符串</param>
        /// <returns>字节数组</returns>
        public static byte[] HexStringToByteArray(this string sourceStr)
        {
            byte[] buf = new byte[sourceStr.Length / 2];
            for (int i = 0; i < buf.Length; i++)
            {
                buf[i] = (byte)(HexCharToByte(sourceStr.Substring(i * 2, 1)) * 0x10 + HexCharToByte(sourceStr.Substring(i * 2 + 1, 1)));
            }
            return buf;
        }

        /// <summary>
        /// 字节数组转十六进制字符串(字母为小写)
        /// </summary>
        /// <param name="sourceArray">源字节数组</param>
        /// <returns>十六进制字符串</returns>
        public static string ByteArrayToHexString(this byte[] sourceArray)
        {
            StringBuilder sBuilder = new StringBuilder();
            for (int i = 0; i < sourceArray.Length; i++)
            {
                sBuilder.Append(sourceArray[i].ToString("x2"));
            }
            return sBuilder.ToString();
        }

        /// <summary>
        /// 将URL的QUERY转化为DICT
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ToQueryDict(this string source)
        {
            source = source.Replace("%3D", "=");
            source = source.Replace("&amp;", "&");
            source = source.Substring(source.IndexOf("?", StringComparison.Ordinal) + 1);
            Dictionary<string, string> dict = new Dictionary<string, string>();
            string[] arrParam = source.Split('&');
            foreach (var sParam in arrParam)
            {
                var i = sParam.IndexOf('=');
                if (i >= 0)
                {
                    if (!dict.ContainsKey(sParam.Substring(0, i)))
                    {
                        dict.Add(sParam.Substring(0, i), sParam.Substring(i + 1)); //aa=a
                    }
                }
            }
            return dict;
        }

        public static string ShowLength(this string source, int len, string ext = "...")
        {
            if (len <= 0 || source.Length < len)
                return source;
            return source.Substring(0, len) + ext;
        }

        public static string RemoveBlank(this string source)
        {
            return source.Trim()
                .Replace("&nbsp;", "")
                .Replace(" ", "").Replace("\\n", "")
                .Replace("\\r", "").Replace("\\t", "");
        }

        /// <summary>
        /// JSON 格式化显示
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string ToJson(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "";
            try
            {
                return JsonConvert.SerializeObject(JsonConvert.DeserializeObject(source), Formatting.Indented);
            }
            catch (Exception e)
            {
                throw e;
                return source;
            }
        }

        /// <summary>
        /// Host
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetHost(this string source)
        {
            if (string.IsNullOrWhiteSpace(source))
                return "";
            try
            {
                return source.Replace("http://", "").Replace("https://", "").Split('/').FirstOrDefault();
            }
            catch (Exception e)
            {
                return "";
            }
        }
    }
    public static class StringValueExtension
    {
        public static bool IsNullOrEmpty(this StringValues s)
        {
            return s.IsNull() || s.ToString().IsNullOrEmpty();
        }

        public static string ToDefault(this StringValues s, string def = "")
        {
            if (s.IsNullOrEmpty())
                return def;
            return s.ToString();
        }
    }
}
