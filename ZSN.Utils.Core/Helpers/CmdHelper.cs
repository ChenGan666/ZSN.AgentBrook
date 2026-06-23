using System.Diagnostics;

namespace ZSN.Utils.Core.Util
{
    public class CmdHelper
    {
        private readonly Process _proc;

        public CmdHelper()
        {
            _proc = new Process();
        }

        public void Exe(string cmd)
        {
            _proc.StartInfo.CreateNoWindow = true;
            _proc.StartInfo.FileName = "cmd.exe";
            _proc.StartInfo.UseShellExecute = false;
            _proc.StartInfo.RedirectStandardInput = true;
            _proc.StartInfo.RedirectStandardOutput = true;
            _proc.StartInfo.RedirectStandardError = true;
            _proc.Start();
            var cmdWriter = _proc.StandardInput;
            _proc.BeginOutputReadLine();
            if (!string.IsNullOrEmpty(cmd))
            {
                cmdWriter.WriteLine(cmd);
            }
            cmdWriter.Close();
            _proc.Close();
        }
    }
}