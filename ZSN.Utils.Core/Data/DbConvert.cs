using System;
using ZSN.Utils.Core.Extensions;

namespace ZSN.Utils.Core.Data
{

    /// <summary>
    ///     数据库字段值类型转换
    /// </summary>
    public static class DbConvert
    {
        /// <summary>
        ///     转成Int16
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">转换失败时提供的默认值，默认为：-1</param>
        /// <returns></returns>
        public static short ToInt16(object dbValue, short defaultValue = -1)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return defaultValue;
            return dbValue.ToString().ToInt16(defaultValue);
        }

        /// <summary>
        ///     转成Int32
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">转换失败时提供的默认值，默认为：-1</param>
        /// <returns></returns>
        public static int ToInt32(object dbValue, int defaultValue = -1)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return defaultValue;
            return dbValue.ToString().ToInt32(defaultValue);
        }

        public static int? ToInt32Nullable(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;
            if (dbValue.ToString() == "")
                return null;
            return dbValue.ToString().ToInt32();
        }
        /// <summary>
        ///     转成Int64
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">转换失败时提供的默认值，默认为：-1</param>
        /// <returns></returns>
        public static long ToInt64(object dbValue, long defaultValue = -1)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return defaultValue;
            return dbValue.ToString().ToInt64(defaultValue);
        }

        public static long? ToInt64Nullable(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;
            if (dbValue.ToString() == "")
                return null;
            return dbValue.ToString().ToInt64();
        }

        /// <summary>
        ///     转成DateTime,转换失败时提供的默认值，默认为：1970-1-1
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <returns></returns>
        public static DateTime ToDateTime(object dbValue)
        {
            return dbValue.ToString().ToDateTime(new DateTime(1970, 1, 1));
        }

        /// <summary>
        ///     转成DateTime
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">转换失败时提供的默认值，默认为：DateTime.MinValue</param>
        /// <returns></returns>
        public static DateTime ToDateTime(object dbValue, DateTime defaultValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return defaultValue;
            return dbValue.ToString().ToDateTime(defaultValue);
        }

        /// <summary>
        ///     转成可为空的DateTime
        /// </summary>
        /// <param name="dbValue"></param>
        /// <returns></returns>
        public static DateTime? ToDateTimeNullable(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;
            if (dbValue.ToString() == "")
                return null;
            return ToDateTime(dbValue);
        }

        /// <summary>
        ///     转成Decimal
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">转换失败时提供的默认值，默认为：-1</param>
        /// <returns></returns>
        public static decimal ToDecimal(object dbValue, decimal defaultValue = -1)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return defaultValue;
            return dbValue.ToString().ToDecimal(defaultValue);
        }

        public static decimal? ToDecimalNullable(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;
            if (dbValue.ToString() == "")
                return null;
            return dbValue.ToString().ToDecimal();
        }

        /// <summary>
        ///     转成String
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">转换失败时提供的默认值，默认为：""</param>
        /// <returns></returns>
        public static string ToString(object dbValue, string defaultValue = "")
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return defaultValue;
            return dbValue.ToString();
        }

        /// <summary>
        ///     转成Byte[]
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <returns></returns>
        public static byte[] ToBytes(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return new byte[0];
            return (byte[])dbValue;
        }

        /// <summary>
        ///     转换成double
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public static double ToDouble(object dbValue, double defaultValue = 0)
        {
            if (dbValue == null || dbValue == DBNull.Value)
            {
                return defaultValue;
            }
            return dbValue.ToString().ToDouble();
        }

        /// <summary>
        ///     转换成byte类型
        /// </summary>
        /// <param name="dbValue">数字库字段值</param>
        /// <param name="defaultValue">默认值</param>
        /// <returns></returns>
        public static byte ToByte(object dbValue, byte defaultValue = 0)
        {
            if (dbValue == null || dbValue == DBNull.Value)
            {
                return defaultValue;
            }
            return Convert.ToByte(dbValue);
        }

        public static short? ToShortNullable(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;
            if (dbValue.ToString() == "")
                return null;
            return Convert.ToInt16(dbValue);
        }

        public static bool? ToBoolNullable(object dbValue)
        {
            if (dbValue == null || dbValue == DBNull.Value)
                return null;
            if (dbValue.ToString() == "")
                return null;
            return dbValue.ToString().ToBoolean();
        }

        public static bool ToBool(object dbValue)
        {
            return dbValue.ToString().ToBoolean();
        }
    }

}