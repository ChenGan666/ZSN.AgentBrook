using ZSN.AI.Core.Common.DependencyInjection;
using ZSN.AI.Core.Interface;
using ZSN.AI.Entity.Model;
using ZSN.AI.Entity.Model.Constant;
using ZSN.AI.Entity.Model.KmsDetail;

using ZSN.AI.Core.Repositories;
using Microsoft.Extensions.Logging;
using Microsoft.KernelMemory;
using Microsoft.KernelMemory.Handlers;
using System.Text;
using ZSN.AI.Core.Handler;
using ZSN.AI.Core.Common.Excel;
using ZSN.AI.BLL;
using System.Threading.Tasks;

namespace ZSN.AI.Core.Service
{
    [ServiceDescription(typeof(IImportKMSService), ServiceLifetime.Scoped)]
    public class ImportKMSService(
        IKMService _kMService,
        IKmsDetails_Repositories _kmsDetails_Repositories,
        IKmss_Repositories _kmss_Repositories,
        ILogger<ImportKMSService> _logger
        ) : IImportKMSService
    {

        public async Task<ImportKMSTaskReq> ImportKMSTask(ImportKMSTaskReq req)
        {
            var km = KnowledgeBaseInfoBussiness.GetModel(req.KmsId);
            try
            {
                var _memory = _kMService.GetMemoryByKMS(km.KnowledgeBaseID);
                string fileid = req.KnowledgeBaseFile.FileID;
                List<string> step = new List<string>();
                if (req.IsQA)
                {
                    _memory.Orchestrator.AddHandler<TextExtractionHandler>("extract_text");
                    _memory.Orchestrator.AddHandler<QAHandler>(km.PreprocessModelID.ToString());
                    _memory.Orchestrator.AddHandler<GenerateEmbeddingsHandler>("generate_embeddings");
                    _memory.Orchestrator.AddHandler<SaveRecordsHandler>("save_memory_records");
                    step.Add("extract_text");
                    step.Add(km.PreprocessModelID.ToString());
                    step.Add("generate_embeddings");
                    step.Add("save_memory_records");
                }

                switch (req.ImportType)
                {
                    case ImportType.File:
                        {
                            //导入文件
                            if (req.IsQA)
                            {
                                var importResult = _memory.ImportDocumentAsync(new Document(fileid)
                                .AddFile(req.FilePath)
                                .AddTag(KmsConstantcs.KmsIdTag, req.KmsId)
                                , index: KmsConstantcs.KmsIndex, steps: step.ToArray()).Result;
                            }
                            else
                            {
                                var importResult = _memory.ImportDocumentAsync(new Document(fileid)
                                 .AddFile(req.FilePath)
                                 .AddTag(KmsConstantcs.KmsIdTag, req.KmsId)
                             , index: KmsConstantcs.KmsIndex).Result;
                            }
                            //查询文档数量
                            var docTextList = await _kMService.GetDocumentByFileID(km.KnowledgeBaseID, fileid);
                            string fileGuidName = Path.GetFileName(req.FilePath);
                            //req.KnowledgeBaseFile.FileName = req.FileName;
                            req.KnowledgeBaseFile.DataCount = docTextList.Count;

                        }
                        break;
                    case ImportType.Url:
                        {
                            //导入url                  
                            if (req.IsQA)
                            {
                                var importResult = await _memory.ImportWebPageAsync(req.Url, fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                                , index: KmsConstantcs.KmsIndex, steps: step.ToArray());
                            }
                            else
                            {
                                var importResult = await _memory.ImportWebPageAsync(req.Url, fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                                , index: KmsConstantcs.KmsIndex);
                            }
                            //查询文档数量
                            var docTextList = await _kMService.GetDocumentByFileID(km.KnowledgeBaseID, fileid);
                            req.KnowledgeBaseFile.Url = req.Url;
                            req.KnowledgeBaseFile.DataCount = docTextList.Count;
                        }
                        break;
                    case ImportType.Text:
                        //导入文本
                        {
                            string? importResult = req.KnowledgeBaseFile.FileID;
                            if (req.IsQA)
                            {
                                importResult = await _memory.ImportTextAsync(req.Text, fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                               , index: KmsConstantcs.KmsIndex, steps: step.ToArray());
                            }
                            else
                            {
                                importResult = await _memory.ImportTextAsync(req.Text, fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                                  , index: KmsConstantcs.KmsIndex);
                            }
                            //查询文档数量
                            var docTextList = await _kMService.GetDocumentByFileID(km.KnowledgeBaseID, fileid);
                            req.KnowledgeBaseFile.Url = req.Url;
                            req.KnowledgeBaseFile.DataCount = docTextList.Count;

                            req.KnowledgeBaseFile.FileID = importResult;
                        }
                        break;
                    case ImportType.Excel:
                        using (var fs = File.OpenRead(req.FilePath))
                        {
                            var excelList = ExeclHelper.ExcelToList<KMSExcelModel>(fs);
                            _memory.Orchestrator.AddHandler<TextExtractionHandler>("extract_text");
                            _memory.Orchestrator.AddHandler<KMExcelHandler>("excel_split");
                            _memory.Orchestrator.AddHandler<GenerateEmbeddingsHandler>("generate_embeddings");
                            _memory.Orchestrator.AddHandler<SaveRecordsHandler>("save_memory_records");

                            StringBuilder text = new StringBuilder();
                            foreach (var item in excelList)
                            {
                                text.AppendLine(@$"Question:{item.Question}{Environment.NewLine}Answer:{item.Answer}{KmsConstantcs.KMExcelSplit}");
                            }
                            var importResult = await _memory.ImportTextAsync(text.ToString(), fileid, new TagCollection() { { KmsConstantcs.KmsIdTag, req.KmsId } }
                                  , index: KmsConstantcs.KmsIndex,
                                  steps: new[]
                                  {
                                        "extract_text",
                                        "excel_split",
                                        "generate_embeddings",
                                        "save_memory_records"
                                  }
                                  );
                            req.KnowledgeBaseFile.FileName = req.FileName;
                            string fileGuidName = Path.GetFileName(req.FilePath);

                            req.KnowledgeBaseFile.DataCount = excelList.Count();

                        }
                        break;
                }
                req.KnowledgeBaseFile.SystemStatus = ZSN.AI.Entity.Model.Enum.ImportKmsStatus.Success;

                KnowledgeBaseInfoBussiness.Update(km);

                _logger.LogInformation("后台导入任务成功:" + req.KnowledgeBaseFile.DataCount);
            }
            catch (Exception ex)
            {
                req.KnowledgeBaseFile.SystemStatus = ZSN.AI.Entity.Model.Enum.ImportKmsStatus.Fail;

                KnowledgeBaseInfoBussiness.Update(km);

                _logger.LogError("后台导入任务异常:" + ex.Message);
            }
            return req;
        }
    }
}
