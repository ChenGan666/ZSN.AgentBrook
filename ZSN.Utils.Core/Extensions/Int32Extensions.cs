using System;

namespace ZSN.Utils.Core.Extensions
{
    public static class Int32Extensions
    {
        /// <summary>
        ///     将Int32值转成指定的枚举类型
        /// </summary>
        /// <typeparam name="T">枚举类型</typeparam>
        /// <param name="source">源枚举值</param>
        /// <param name="defaultValue">如果转换失败，返回默认的枚举项</param>
        /// <returns></returns>
        public static T ToEnum<T>(this int source, T defaultValue)
        {
            var source2 = source.ToString();
            if (!string.IsNullOrEmpty(source2))
            {
                try
                {
                    var value = (T)Enum.Parse(typeof(T), source2, true);
                    return value;
                }
                catch
                {
                    // ignored
                }
            }
            return defaultValue;
        }

        /// <summary>
        ///     yyyyMMdd 的时间码转为DateTime
        /// </summary>
        /// <param name="d"></param>
        /// <returns></returns>
        public static DateTime DateCodeToDateTime(this int d)
        {
            return new DateTime(d / 10000, d / 100 % 100, d % 100);
        }
    }
}