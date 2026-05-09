using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using static ZSN.AI.Core.Service.OllamaService;

namespace ZSN.AI.Core.Interface
{
    public interface IOllamaService
    {
        public event LogMessageHandler LogMessageReceived;
        Task StartOllama(string modelName);
    }
}
