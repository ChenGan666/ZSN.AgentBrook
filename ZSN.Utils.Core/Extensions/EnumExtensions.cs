using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace ZSN.Utils.Core.Extensions
{
    public static class EnumExtensions
    {
        /// <summary>
        ///     将枚举转换为数字
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static int ToInt32(this Enum source)
        {
            return Convert.ToInt32(source);
        }

        /// <summary>
        ///     获取枚举值的描述信息
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetDescription(this Enum source)
        {
            var typeDescription = typeof(DescriptionAttribute);
            var fields = source.GetType().GetFields();
            var strText = string.Empty;
            foreach (var field in fields)
            {
                if (field.FieldType.IsEnum && field.Name.Equals(source.ToString()))
                {
                    var arr = field.GetCustomAttributes(typeDescription, true);
                    if (arr.Length > 0)
                    {
                        var aa = (DescriptionAttribute)arr[0];
                        strText = aa.Description;
                    }
                    else
                    {
                        strText = field.Name;
                    }
                    break;
                }
            }
            return strText;
        }

        /// <summary>
        ///     获取枚举值的特征名称
        /// </summary>
        /// <param name="source"></param>
        /// <returns></returns>
        public static string GetDisplayName(this Enum source)
        {
            var typeDescription = typeof(DisplayAttribute);
            var fields = source.GetType().GetFields();
            var strText = string.Empty;
            foreach (var field in fields)
            {
                if (field.FieldType.IsEnum && field.Name.Equals(source.ToString()))
                {
                    var arr = field.GetCustomAttributes(typeDescription, true);
                    if (arr.Length > 0)
                    {
                        var aa = (DisplayAttribute)arr[0];
                        strText = aa.Name;
                    }
                    else
                    {
                        strText = field.Name;
                    }
                    break;
                }
            }
            return strText;
        }
    }
}