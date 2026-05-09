namespace ZSN.AgentBrowser
{
    /// <summary>
    /// 命令执行结果
    /// </summary>
    public class CommandResult
    {
        public bool Success { get; set; }
        public string Output { get; set; } = string.Empty;
        public string Error { get; set; } = string.Empty;
        public int ExitCode { get; set; }

        public override string ToString()
        {
            return Success 
                ? $"Success: {Output}" 
                : $"Error: {Error}";
        }
    }
}
