using System;

namespace ZSN.Utils.Core.Helpers
{
    public class ConsoleLogHelper
    {
        public static void WriteLine(object obj, ConsoleColor color)
        {
            Console.ResetColor();
            Console.ForegroundColor = color;
            WriteLine(obj);
        }

        public static void WriteLine(object obj = null)
        {
            var str = obj?.ToString() ?? "";
            Console.WriteLine(str);
            NLogHelper.WriteCustom(str, "/ConsoleLog/");
            Console.ResetColor();
        }
    }
}
