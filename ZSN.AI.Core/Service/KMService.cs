
using DocumentFormat.OpenXml.Drawing.Diagrams;
using DocumentFormat.OpenXml.EMMA;
using Elastic.Transport;
using LLama;
using Markdig;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.AI;
using Microsoft.KernelMemory.AI.Ollama;
using Microsoft.KernelMemory.Configuration;
using Microsoft.KernelMemory.DataFormats;
using Microsoft.KernelMemory.DocumentStorage.DevTools;
using Microsoft.KernelMemory.FileSystem.DevTools;
using Microsoft.KernelMemory.MemoryStorage;
using Microsoft.KernelMemory.MemoryStorage.DevTools;
using Microsoft.KernelMemory.Postgres;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

using ZSN.AI.BLL;
using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Utils;
using ZSN.AI.DAL;
using ZSN.AI.Entity;
using ZSN.AI.Entity.Model.Constant;
using ZSN.AI.Entity.Object;
using ZSN.AI.Entity.Options;
using ZSN.Utils.Core.Helpers;
using ServiceLifetime = ZSN.AI.Core.Common.DependencyInjection.ServiceLifetime;

namespace ZSN.AI.Core.Service
{
    [ServiceDescription(typeof(IKMService), ServiceLifetime.Scoped)]
    public class KMService(
        IKernelService _kernelService
    ) : IKMService
    {
        private MemoryServerless _memory;

        private List<UploadFileItem> _fileList = [];

        public List<UploadFileItem> FileList => _fileList;

        public async IAsyncEnumerable<string> TestEmbeddingAsync(LargeModelConfig ModelConfig, string questions)
        {
            var embeddingHttpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(ModelConfig.Model.EndPoint);
            
            var config = new OllamaConfig
            {
                Endpoint = ModelConfig.Model.EndPoint,
                EmbeddingModel = new OllamaModelConfig(ModelConfig.Model.ModelName, 2048)
            };

            var memory =  new KernelMemoryBuilder()
                .WithOllamaTextGeneration(config, new CL100KTokenizer())
                .WithOllamaTextEmbeddingGeneration(config, new CL100KTokenizer())
                .Build();
            

            yield return await memory.ImportTextAsync(questions);
        }

        public MemoryServerless GetMemory(LargeModelUnit ModelUnit,string KnowledgeBaseID)
        {
            var chatModel = ModelUnit.ChatModel;
            var embedModel = ModelUnit.VectorModel;
            var chatHttpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(chatModel.Model.EndPoint);
            var embeddingHttpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(embedModel.Model.EndPoint);
            SearchClientConfig searchClientConfig;
            if (ModelUnit.RerankModel.Model.LargeModelID == 0)
            {
                //不重排直接取查询数
                searchClientConfig = new SearchClientConfig
                {
                    MaxAskPromptSize = ModelUnit.ChatModel.MaxAskPromptSize,
                    MaxMatchesCount = ModelUnit.ChatModel.MaxMatchesCount,
                    AnswerTokens = ModelUnit.ChatModel.AnswerTokens,
                    EmptyAnswer = KmsConstantcs.KmsSearchNull
                };
            }
            else 
            {
                //重排取rerank数
                searchClientConfig = new SearchClientConfig
                {
                    MaxAskPromptSize = ModelUnit.ChatModel.MaxAskPromptSize,
                    MaxMatchesCount = ModelUnit.ChatModel.RerankCount,
                    AnswerTokens = ModelUnit.ChatModel.AnswerTokens,
                    EmptyAnswer = KmsConstantcs.KmsSearchNull
                };
            }
           

            var memoryBuild = new KernelMemoryBuilder()
                  .WithSearchClientConfig(searchClientConfig);

            //加载会话模型
            WithTextGenerationByAIType(memoryBuild, chatModel.Model, chatHttpClient);
            //加载向量模型
            WithTextEmbeddingGenerationByAIType(memoryBuild, embedModel.Model, embeddingHttpClient);
            //加载向量库
            WithMemoryDbByVectorDB(memoryBuild, KnowledgeBaseID);

            _memory = memoryBuild.AddSingleton<IKernelService>(_kernelService).Build<MemoryServerless>();
            return _memory;
        }

        public MemoryServerless GetMemoryByKMS(string KnowledgeBaseID)
        {

            //获取知识库配置
            var kms = KnowledgeBaseInfoBussiness.GetModel(KnowledgeBaseID);
            var chatModel = LargeModelInfoBussiness.GetModel(kms.PreprocessModelID);
            var embedModel = LargeModelInfoBussiness.GetModel(kms.VectorModelID);

            //http代理
            var chatHttpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(chatModel.EndPoint);
            var embeddingHttpClient = OpenAIHttpClientHandlerUtil.GetHttpClient(embedModel.EndPoint);

            var memoryBuild = new KernelMemoryBuilder()
                .WithCustomTextPartitioningOptions(new TextPartitioningOptions
                {
                    //MaxTokensPerLine = kms.LineSliceCount,
                    MaxTokensPerParagraph = kms.ParagraphSlice,
                    OverlappingTokens = kms.OverlapSection
                });

            //加载会话模型
            WithTextGenerationByAIType(memoryBuild, chatModel, chatHttpClient);
            //加载向量模型
            WithTextEmbeddingGenerationByAIType(memoryBuild, embedModel, embeddingHttpClient);
            //加载向量库
            WithMemoryDbByVectorDB(memoryBuild, KnowledgeBaseID);

            _memory = memoryBuild.AddSingleton<IKernelService>(_kernelService).Build<MemoryServerless>();
            
            return _memory;

        }


        private void WithTextEmbeddingGenerationByAIType(IKernelMemoryBuilder memory, LargeModelInfo embedModel,HttpClient embeddingHttpClient)
        {
            switch (embedModel.ModelOrganizationID)
            {
                case ZSN.AI.Entity.Model.Enum.AIType.OpenAI:
                    memory.WithOpenAITextEmbeddingGeneration(new OpenAIConfig()
                    {
                        APIKey = embedModel.ModelKey,
                        EmbeddingModel = embedModel.ModelName,
                        Endpoint = embedModel.EndPoint
                    }, null, false, embeddingHttpClient);
                    break;
                    
                case ZSN.AI.Entity.Model.Enum.AIType.Ollama:
                    var config = new OllamaConfig
                    {
                        Endpoint = embedModel.EndPoint,
                        EmbeddingModel = new OllamaModelConfig(embedModel.ModelName, 2048)
                    };
                    memory.WithOllamaTextEmbeddingGeneration(config, new CL100KTokenizer());
                    break;

                default:
                    // 处理其他兼容 OpenAI API 的嵌入模型，如 Bigmodel, QWen, DeepSeek 等
                    memory.WithOpenAITextEmbeddingGeneration(new OpenAIConfig()
                    {
                        APIKey = embedModel.ModelKey,
                        EmbeddingModel = embedModel.ModelName,
                        Endpoint = embedModel.EndPoint
                    }, null, false, embeddingHttpClient);
                    break;
                    
            }
        }

        private void WithTextGenerationByAIType(IKernelMemoryBuilder memory, LargeModelInfo chatModel,
            HttpClient chatHttpClient)
        {
            switch (chatModel.ModelOrganizationID)
            {
                
                case ZSN.AI.Entity.Model.Enum.AIType.OpenAI:
                    memory.WithOpenAITextGeneration(new OpenAIConfig()
                    {
                        APIKey = chatModel.ModelKey,
                        TextModel = chatModel.ModelName,
                        Endpoint = chatModel.EndPoint
                    }, null, chatHttpClient);
                    break;

                case ZSN.AI.Entity.Model.Enum.AIType.Ollama:
                    memory.WithOllamaTextGeneration(chatModel.ModelName, chatModel.EndPoint);
                    break;

                default:
                    // 处理其他兼容 OpenAI API 的模型，如 Bigmodel, QWen, DeepSeek 等
                    memory.WithOpenAITextGeneration(new OpenAIConfig()
                    {
                        APIKey = chatModel.ModelKey,
                        TextModel = chatModel.ModelName,
                        Endpoint = chatModel.EndPoint
                    }, null, chatHttpClient);
                    break;
            }
        }

        private void WithMemoryDbByVectorDB(IKernelMemoryBuilder memory,string KnowledgeBaseID)
        {
            DbInfo dbInfo = DbConfig.GetDbInfo("KnowledgeBaseDb");
            string VectorDb = dbInfo.DbType;
            string ConnectionString = dbInfo.ConnectionString;
            string TableNamePrefix = dbInfo.TableNamePrefix;

            switch (VectorDb)
            {
                case "Postgres":
                    memory.WithPostgresMemoryDb(new PostgresConfig()
                    {
                        ConnectionString = ConnectionString,
                        Schema = "public",
                        TableNamePrefix = TableNamePrefix + KnowledgeBaseID,
                    });
                    memory.WithSimpleFileStorage(new SimpleFileStorageConfig
                    { 
                        StorageType = FileSystemTypes.Disk, 
                        Directory = Path.Combine(AppContext.BaseDirectory, "km-files")
                    });
                    break;

                case "Disk":
                    memory.WithSimpleVectorDb(new SimpleVectorDbConfig()
                    {
                        StorageType = FileSystemTypes.Disk,
                    });
                    break;

                case "Memory":
                    memory.WithSimpleVectorDb(new SimpleVectorDbConfig()
                    {
                        StorageType = FileSystemTypes.Volatile
                    });
                    break;

                case "Qdrant":
                    var qdrantConfig = ConnectionString.Split("|");
                    memory.WithQdrantMemoryDb(qdrantConfig[0], qdrantConfig[1]);
                    break;

                case "Redis":
                    memory.WithRedisMemoryDb(new RedisConfig()
                    {
                        ConnectionString = ConnectionString,
                    });
                    break;
            }
        }

        public async Task<List<KMFile>> GetDocumentByFileID(string kmsId, string fileId)
        {
            var memory = GetMemoryByKMS(kmsId);
            var memories = await memory.ListIndexesAsync();
            var memoryDbs = memory.Orchestrator.GetMemoryDbs();
            var docTextList = new List<KMFile>();

            foreach (var memoryIndex in memories)
            {
                foreach (var memoryDb in memoryDbs)
                {
                    await foreach (var item in memoryDb.GetListAsync(memoryIndex.Name, new List<MemoryFilter>() { new MemoryFilter().ByDocument(fileId) }, 1000, true))
                    {
                        docTextList.Add(new KMFile()
                        {
                            DocumentId = item.GetDocumentId(),
                            Text = item.GetPartitionText(),
                            Url = item.GetWebPageUrl(KmsConstantcs.KmsIndex),
                            LastUpdate = item.GetLastUpdate().LocalDateTime.ToString("yyyy-MM-dd HH:mm:ss"),
                            File = item.GetFileName()
                        });
                    }
                }
            }

            return docTextList;
        }

        public async Task<List<RelevantSource>> GetRelevantSourceList(LargeModelUnit ModelUnit, string msg,string KnowledgeBaseID)
        {
            var result = new List<RelevantSource>();
            if (string.IsNullOrWhiteSpace(KnowledgeBaseID))
                return result;

            var memory = GetMemoryByKMS(KnowledgeBaseID);// GetMemory(ModelUnit, KnowledgeBaseID);

            var filter = new MemoryFilter().ByTag(KmsConstantcs.KmsIdTag, KnowledgeBaseID);

            var searchResult = await memory.SearchAsync(msg, index: KmsConstantcs.KmsIndex, filter: filter);
            if (!searchResult.NoResult)
            {
                foreach (var item in searchResult.Results)
                {
                    result.AddRange(item.Partitions.Select(part => new RelevantSource()
                    {
                        SourceName = item.SourceName,
                        Text = part.Text,
                        Relevance = part.Relevance
                    }));
                }
            }

            return result;
        }

        public bool BeforeUpload(UploadFileItem file)
        {
            List<string> types = new List<string>() {
                "text/plain",
                "application/msword",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.ms-excel",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.ms-powerpoint",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation",
                "application/pdf",
                "application/json",
                "text/x-markdown",
                "text/markdown",
                "image/jpeg",
                "image/png",
                "image/tiff"
            };

            string[] exceptExts = [".md", ".pdf"];
            var validTypes = types.Contains(file.Type) || exceptExts.Contains(file.Ext);
            if (!validTypes && file.Ext != ".md")
            {
            }
            var IsLt500K = file.Size < 1024 * 1024 * 100;
            if (!IsLt500K)
            {
            }

            return validTypes && IsLt500K;
        }

        public void OnSingleCompleted(UploadInfo fileinfo)
        {
            if (fileinfo.File.State == UploadState.Success)
            {
                //文件列表
                _fileList.Add(new UploadFileItem()
                {
                    FileName = fileinfo.File.FileName,
                    Url = fileinfo.File.Url = fileinfo.File.Response
                });
            }
        }
    }
}
