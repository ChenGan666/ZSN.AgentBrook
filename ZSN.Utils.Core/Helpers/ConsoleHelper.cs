using System;
using System.Threading.Tasks;

namespace ZSN.Utils.Core.Helpers
{
    public static class ConsoleHelper
    {
        #region 控制台日志

        public static void WriteInfo(string message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            ConsoleLogHelper.WriteLine("#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
        }

        public static Task WriteInfoAsync(string message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Yellow;
            return Console.Out.WriteLineAsync("#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
        }

        public static void WriteSuccess(string message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Green;
            ConsoleLogHelper.WriteLine("#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
        }

        public static Task WriteSuccessAsync(string message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Green;
            return Console.Out.WriteLineAsync("#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
        }

        public static void WriteError(string message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            ConsoleLogHelper.WriteLine("#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
        }

        public static Task WriteErrorAsync(string message)
        {
            Console.ResetColor();
            Console.ForegroundColor = ConsoleColor.Red;
            return Console.Out.WriteLineAsync("#" + DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss") + ": " + message);
        }

        #endregion
    }
}