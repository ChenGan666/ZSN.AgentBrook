namespace ZSN.Utils.Core.Extensions
{
    public static class Int64Extensions
    {
        /// <summary>
        ///     将指定的长整值转换为对应的字节大小
        /// </summary>
        /// <param name="fileSize"></param>
        /// <returns></returns>
        public static string ToFileSize(this long fileSize)
        {
            if (fileSize < 0x400L)
            {
                return $"{fileSize}Byte";
            }
            if (fileSize >= 0x400L && fileSize <= 0x100000L)
            {
                return $"{fileSize * 1.0 / 0x400L:F2}".Trim('0').Trim('.') + "KB";
            }
            if (fileSize >= 0x100000L && fileSize <= 0x40000000L)
            {
                return $"{fileSize * 1.0 / 0x100000L:F2}".Trim('0').Trim('.') + "MB";
            }
            if (fileSize >= 0x40000000L)
            {
                return $"{fileSize * 1.0 / 0x40000000L:F2}".Trim('0').Trim('.') + "GB";
            }
            return "";
        }
    }
}