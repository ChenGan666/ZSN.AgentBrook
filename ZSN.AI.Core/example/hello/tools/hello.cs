using System;

// 说明：该文件用于“发现/解析”演示。实际执行建议使用 hello.csx 或 hello.ps1。
// 如果确需执行 .cs 文件，请确保使用 dotnet-script 并允许扩展名 .cs。

class Program
{
    public static int Main(string[] args)
    {
        string name = "World";
        if (args != null && args.Length > 0)
        {
            // 简单参数解析：优先匹配 -Name <value>，否则第一个参数作为名称
            for (int i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], "-Name", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                {
                    name = args[i + 1];
                    break;
                }
            }
            if (name == "World") name = args[0];
        }

        Console.WriteLine($"Hello, {name} from C#");
        return 0;
    }
}
