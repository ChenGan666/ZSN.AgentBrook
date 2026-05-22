using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.Core.Common.DependencyInjection;

namespace ZSN.AI.Core.Service
{
    [ServiceDescription(typeof(IOllamaService), ServiceLifetime.Singleton)]
    public class OllamaService : IOllamaService
    {
        private Process process;
        public delegate Task LogMessageHandler(string message);
        public event LogMessageHandler LogMessageReceived;
        protected virtual async Task OnLogMessageReceived(string message)
        {
            LogMessageReceived?.Invoke(message);
        }

        public async Task StartOllama(string modelName)
        {
            Console.OutputEncoding = Encoding.UTF8;
            var cmdTask = Task.Factory.StartNew(() =>
            {

                var isProcessComplete = false;

                process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "ollama",
                        Arguments = "run " + modelName,
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                    }
                };
                process.OutputDataReceived += (sender, eventArgs) =>
                {
                    //Console.WriteLine($"{eventArgs.Data.ConvertToString()}");
                    if (!eventArgs.Data.ConvertToString().Contains("The handle is invalid"))
                    {
                        OnLogMessageReceived(eventArgs.Data.ConvertToString());
                    }
                };
                process.ErrorDataReceived += (sender, eventArgs) =>
                {
                    Console.WriteLine($"{eventArgs.Data.ConvertToString()}");
                    if (!eventArgs.Data.ConvertToString().Contains("The handle is invalid"))
                    {
                        OnLogMessageReceived(eventArgs.Data.ConvertToString());
                    }
                };
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                process.WaitForExit();

                OnLogMessageReceived("--------------------完成--------------------");
            }, TaskCreationOptions.LongRunning);
            await cmdTask;
        }

    }
}
