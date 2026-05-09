
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using ModelContextProtocol.Client;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model;

namespace ZSN.AI.Core.Interface
{
    public interface IKernelService
    {
        Kernel GetKernel(LargeModelInfo ModelInfo);
        Kernel GetKernelByAIModelID(int modelid);
        void ImportFunctions(LargeModelConfig ModelConfig, Kernel _kernel);
        void ImportFunctions(Kernel _kernel, object type, string pluginName);
        void ImportFunctions(Kernel _kernel, IList<McpClientTool> clientTools);
        Task<string> HistorySummarize(Kernel _kernel, ChatHistory history);
        Task<string> PromptFunctionCall(Kernel _kernel, CallFunction callFunction, KernelArguments parameter);

        /// <summary>
        /// 生成文本向量嵌入
        /// </summary>
        /// <param name="modelInfo">向量模型信息</param>
        /// <param name="text">要生成向量的文本</param>
        /// <returns>向量数组</returns>
        Task<float[]> GenerateEmbeddingAsync(LargeModelInfo modelInfo, string text);
    }
}
