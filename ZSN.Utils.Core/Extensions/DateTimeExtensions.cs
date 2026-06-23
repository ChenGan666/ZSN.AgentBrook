using System;
using System.Globalization;

namespace ZSN.Utils.Core.Extensions
{
    public static class DateTimeExtensions
    {
        /// <summary>
        ///     转换为JS需要的标准时间
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static double ToUtcJavascriptTime(this DateTime time)
        {
            return
                time.Subtract(DateTime.Parse("1970-1-1")).TotalMilliseconds
                -
                (DateTime.Now - DateTime.UtcNow).TotalMilliseconds;
        }

        /// <summary>
        ///     获取时间与当前时间的描述
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static string ToTimeDescription(this DateTime time)
        {
            var timespan = DateTime.Now - time;
            if (timespan.TotalHours >= 24)
            {
                if (time.Year == DateTime.Now.Year)
                {
                    return time.ToString("MM月dd日");
                }
                return time.ToString("yyyy年MM月dd日");
            }
            if (timespan.TotalMinutes < 60)
            {
                if (timespan.TotalMinutes < 1)
                {
                    return "刚刚";
                }
                return timespan.TotalMinutes.ToString("0") + " 分钟前";
            }
            return timespan.TotalHours.ToString("0") + " 小时前";
        }

        public static string ToUSATime(this DateTime time)
        {
            return time.ToString("MMM dd, yyyy", new CultureInfo("en-US"));
        }

        public static uint ToUnixStamp(this DateTime time)
        {
            var ts = time - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            var uiStamp = Convert.ToUInt32(ts.TotalSeconds);
            return uiStamp;
        }

        public static long ToJsUnixStamp(this DateTime time)
        {
            var ts = time - TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            var uiStamp = Convert.ToInt64(ts.TotalMilliseconds);
            return uiStamp;
        }

        public static DateTime UnixStampToDateTime(this long timespan)
        {
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            DateTime time = startTime.AddSeconds(timespan);
            return time;
        }

        public static DateTime JsUnixStampToDateTime(this long timespan)
        {
            var startTime = TimeZone.CurrentTimeZone.ToLocalTime(new DateTime(1970, 1, 1));
            DateTime time = startTime.AddMilliseconds(timespan);
            return time;
        }
        public static string DateFormat(this object date, string format = "yyyy-MM-dd")
        {
            if (date == null)
                return string.Empty;
            DateTime test;
            if (!date.ToString().IsDateTime(out test))
                return date.ToString();
            return test.ToString(format);
        }


        public static DateTime TicksToDateTime(this long timespan)
        {
            return new DateTime(timespan);     //当前的ticks转为时间类型
        }

        public static int ToDateCode(this DateTime time)
        {
            return Convert.ToInt32(time.ToString("yyyyMMdd"));
        }

        /// <summary>
        /// 获取本周的周一日期
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static DateTime GetThisWeekMonday(this DateTime time)
        {
            DateTime date = DateTime.Now;
            DateTime firstDate = System.DateTime.Now;
            switch (date.DayOfWeek)
            {
                case System.DayOfWeek.Monday:
                    firstDate = date;
                    break;
                case System.DayOfWeek.Tuesday:
                    firstDate = date.AddDays(-1);
                    break;
                case System.DayOfWeek.Wednesday:
                    firstDate = date.AddDays(-2);
                    break;
                case System.DayOfWeek.Thursday:
                    firstDate = date.AddDays(-3);
                    break;
                case System.DayOfWeek.Friday:
                    firstDate = date.AddDays(-4);
                    break;
                case System.DayOfWeek.Saturday:
                    firstDate = date.AddDays(-5);
                    break;
                case System.DayOfWeek.Sunday:
                    firstDate = date.AddDays(-6);
                    break;
            }
            return firstDate;
        }

        public static string ToCurrentTime(this DateTime time)
        {
            return time.ToString("yyyy-MM-dd HH:mm:ss");
        }
        public static string ToCurrentTime(this DateTime? time)
        {
            return time?.ToString("yyyy-MM-dd HH:mm:ss");
        }

        public static DateTime? ToDateTimeOrNull(this string str)
        {
            DateTime result;
            if (DateTime.TryParse(str, out result))
                return new DateTime?(result);
            return new DateTime?();
        }
        public static string ToFormatDate(this string source, string format)
        {
            return source.ToDateTime().ToString(format);
        }
    }
}