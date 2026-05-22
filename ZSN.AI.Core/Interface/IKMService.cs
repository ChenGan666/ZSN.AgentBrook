
using Microsoft.KernelMemory;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Interface
{
    public interface IKMService
    {
        IAsyncEnumerable<string> TestEmbeddingAsync(LargeModelConfig ModelConfig, string questions);
        MemoryServerless GetMemory(LargeModelUnit ModelUnit,string KnowledgeBaseID);

        MemoryServerless GetMemoryByKMS(string kmsID);

        Task<List<KMFile>> GetDocumentByFileID(string kmsId, string fileId);

        Task<List<RelevantSource>> GetRelevantSourceList(LargeModelUnit ModelUnit, string msg, string KnowledgeBaseID);
    }
}