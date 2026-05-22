using ZSN.AI.BLL;
using ZSN.AI.Entity;
using ZSN.Utils.Core.Extensions;
using Microsoft.AspNetCore.Mvc;

using ZSN.Utils.Core.Helpers;
using ZSN.AgentBrook.Web.Manage.Attributes;
using ZSN.AI.Service.Controllers;
using Microsoft.AspNetCore.Components;
using Microsoft.SemanticKernel.ChatCompletion;
using ZSN.AI.Core.Interface;
using ZSN.AI.Core.Models.Image;
using ZSN.AI.Core.Models.Video;
using ZSN.AI.Core.Service;
using System.Text;
using Elastic.Clients.Elasticsearch;
using Markdig;
using ZSN.AI.Core.Repositories;
using ZSN.AI.Core.Utils;
using ZSN.AI.Entity.Model.Enum;

namespace ZSN.AgentBrook.Web.Manage.Areas.Manage.Controllers
{
    [AdminAttributes]
    public class LargeModelController: AdminBaseController
    {

        private readonly IChatService _chatService;
        private readonly IKernelService _kernelService;
        private readonly IImageService _imageService;
        private readonly IVideoService _videoService;
        private readonly IKMService _kMService;

        public LargeModelController(IChatService chatService, IKernelService kernelService, IImageService imageService, IVideoService videoService, IKMService kMService) {
            _chatService = chatService;
            _kernelService = kernelService;
            _imageService = imageService;
            _videoService = videoService;
            _kMService = kMService;
        }

        public IActionResult index(int index = 1, int size = 10)
        {
            var lst = LargeModelInfoBussiness.GetListByPage(size, index, "", out int pagetotal, out int total);
            ViewBag.Index = index;
            ViewBag.Size = size;
            ViewBag.Total = total;
            ViewBag.LargeModelList = lst;
            return View();
        }

        [HttpPost]
        public JsonMsg<string> LargeModelStatus(int mid, bool status)
        {
            var LargeModel = LargeModelInfoBussiness.GetModel(mid);
            LargeModel.SystemStatus = status ? ZSN.AI.Entity.LargeModelStatus.Normal : ZSN.AI.Entity.LargeModelStatus.Disabled;

            LargeModelInfoBussiness.Update(LargeModel);
            return JsonMsg<string>.OK("更新成功");
        }

        [HttpPost]
        public JsonMsg<string> LargeModelIsDefaultModel(int mid, bool status)
        {
            List<LargeModelInfo> lst = LargeModelInfoBussiness.GetList(" IsDefaultModel=1 ");

            foreach (var item in lst)
            {
                item.IsDefaultModel = 0;
                LargeModelInfoBussiness.Update(item);
            }

            var LargeModel = LargeModelInfoBussiness.GetModel(mid);
            LargeModel.IsDefaultModel = status ? 1 : 0;

            LargeModelInfoBussiness.Update(LargeModel);
            return JsonMsg<string>.OK("更新成功");
        }

        public IActionResult Edit(int mid = 0)
        {
            var LargeModel = mid == 0 ? new LargeModelInfo() : LargeModelInfoBussiness.GetModel(mid);
            ViewBag.LargeModel = LargeModel;

            ViewBag.ModelTypeList = (new AIModelTypeList()).List();// BaseDictionaryInfoBussiness.GetChildList("模型类型");
            ViewBag.ModelOrganizationList = (new AIOrganizationList()).List();
            
            ViewBag.SemanticFunction = PluginsInfoBussiness.GetList(" PluginsType = 1 and SystemStatus=0");
            ViewBag.NativeFunction = PluginsInfoBussiness.GetList(" PluginsType = 2 and SystemStatus=0");
            ViewBag.PreviewHost = ConfigHelper.GetString("previewHost");

            return View();
        }
        [HttpPost]
        public JsonMsg<string> LargeModelSave(LargeModelInfo LargeModel)
        {
            if (LargeModel.LargeModelID<=0)
            {
                LargeModel.CreateTime = DateTime.Now;
                LargeModelInfoBussiness.Add(LargeModel);
            }
            else
            {

                LargeModelInfoBussiness.Update(LargeModel);
            }
            return JsonMsg<string>.OK("保存成功");
        }

        public JsonMsg<string> LargeModelDel(string mid)
        {
            LargeModelInfoBussiness.DeleteList(mid);

            return JsonMsg<string>.OK("删除成功");
        }

        [HttpPost]
        public async Task<JsonMsg<List<Chats>>> Test(LargeModelInfo LargeModel)
        {
            LargeModelUnit ModelUnit = new LargeModelUnit();
            ChatHistory history = new ChatHistory();
            List<Chats> MessageList = [];
            Chats info = null;

            string _testStr = "这是一个测试，来确定大模型接口是否联通！";

            history.AddUserMessage(_testStr);

            ModelUnit = ModelUnit.ModelMap(LargeModel.TypeCode, LargeModel);
            IAsyncEnumerable<string> chatResult = null;
            string _res = "";
            LargeModelConfig modelConfig = new LargeModelConfig();

            modelConfig.Model = LargeModel;

            StringBuilder rawContent = new StringBuilder();

            switch (LargeModel.TypeCode)
            {
                case AIModelType.Chat:
                    
                    chatResult = _chatService.SendChatAsync(modelConfig, history);
                    await foreach (var content in chatResult)
                    {
                        if (info == null)
                        {
                            rawContent.Append(content.ConvertToString());
                            info = new Chats();
                            info.Id = Guid.NewGuid().ToString();
                            info.UserName = "_userName";
                            info.AppId = "Test";
                            info.Context = content.ConvertToString();
                            info.CreateTime = DateTime.Now;

                            MessageList.Add(info);
                        }
                        else
                        {
                            rawContent.Append(content.ConvertToString());
                        }
                        info.Context = rawContent.ToString();
                    }

                    break;
                case AIModelType.Embedding:
                    try
                    {
                        var vector = await _kernelService.GenerateEmbeddingAsync(LargeModel, _testStr);
                        var preview = string.Join(", ", vector.Take(10).Select(v => v.ToString("F6")));
                        info = new Chats
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = "_userName",
                            AppId = "Test",
                            Context = $"向量生成测试成功！\n\n" +
                                      $"测试文本：{_testStr}\n" +
                                      $"向量维度：{vector.Length}\n" +
                                      $"前10维值：[{preview}...]",
                            CreateTime = DateTime.Now
                        };
                        MessageList.Add(info);
                    }
                    catch (Exception ex)
                    {
                        info = new Chats
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = "_userName",
                            AppId = "Test",
                            Context = $"向量生成测试失败！\n\n错误信息：{ex.Message}",
                            CreateTime = DateTime.Now
                        };
                        MessageList.Add(info);
                    }
                    break;
                case AIModelType.T2Image:
                case AIModelType.I2Image:
                    // 图像生成测试
                    try
                    {
                        string imagePrompt;
                        string? testImageInput = null;
                        
                        // 根据模型类型决定测试内容
                        if (LargeModel.TypeCode == AIModelType.I2Image)
                        {
                            // 图生图测试：需要提供输入图像
                            imagePrompt = "Convert to quick pencil sketch";
                            
                            // 提供一个小的测试图像（1x1 透明 PNG 的 Base64）
                            // 实际使用时应该提供真实的图像数据
                            testImageInput = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mNk+M9QDwADhgGAWjR9awAAAABJRU5ErkJggg==";
                            
                            Console.WriteLine("[测试] 使用图生图模式，提供测试图像");
                        }
                        else
                        {
                            // 文生图测试：不需要输入图像
                            imagePrompt = "一只可爱的小猫在花园里玩耍，阳光明媚，鲜花盛开";
                            Console.WriteLine("[测试] 使用文生图模式");
                        }
                        
                        // 调用图像生成服务
                        var imageRequest = new ImageGenerationRequest
                        {
                            GenerationType = testImageInput != null ? ImageGenerationType.ImageToImage : ImageGenerationType.TextToImage,
                            Prompt = imagePrompt,
                            ImageInput = testImageInput,
                            Width = 1024,
                            Height = 1024,
                            Quality = "standard",
                            Style = "vivid"
                        };
                        
                        var imageUrl = await _imageService.GenerateImageAsync(LargeModel, imageRequest);

                        // 构建返回消息
                        var testMode = testImageInput != null ? "图生图" : "文生图";
                        info = new Chats
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = "_userName",
                            AppId = "Test",
                            Context = $"✅ 图像生成测试成功！\n\n模式：{testMode}\n提示词：{imagePrompt}\n\n生成的图像URL：\n{imageUrl}\n\n参数：1024x1024",
                            CreateTime = DateTime.Now
                        };
                        
                        MessageList.Add(info);
                    }
                    catch (Exception ex)
                    {
                        // 图像生成失败
                        info = new Chats
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = "_userName",
                            AppId = "Test",
                            Context = $"❌ 图像生成测试失败！\n\n错误信息：{ex.Message}\n\n详细信息：{ex.StackTrace}",
                            CreateTime = DateTime.Now
                        };
                        
                        MessageList.Add(info);
                    }
                    break;
                case AIModelType.Rerank:
                    //ModelUnit.RerankModel.Model = LargeModel;
                    break;
                case AIModelType.I2Video:
                case AIModelType.T2Video:
                    // 视频生成测试
                    try
                    {
                        // 添加模型配置信息日志
                        Console.WriteLine($"[测试] 模型名称: {LargeModel.ModelName}");
                        Console.WriteLine($"[测试] 模型组织: {LargeModel.ModelOrganizationID}");
                        Console.WriteLine($"[测试] 端点: {LargeModel.EndPoint}");
                        Console.WriteLine($"[测试] 模型类型: {LargeModel.TypeCode}");
                        
                        string videoPrompt;
                        string? testImageInput = null;
                        
                        // 根据模型类型决定测试内容
                        if (LargeModel.TypeCode == AIModelType.I2Video)
                        {
                            // 图生视频测试：需要提供输入图像
                            videoPrompt = "让图片中的场景动起来，添加自然的运动效果";
                            
                            testImageInput = "https://api.modelverse.cn/image/d2p7pge43lyniu/output/01c17556-6fe2-483e-a382-38c9b6138162-u1_c1095348-6f25-4185-9ec1-44b349d09e86.jpeg";
                            
                            Console.WriteLine("[测试] 使用图生视频模式，提供测试图像");
                        }
                        else
                        {
                            // 文生视频测试：不需要输入图像
                            videoPrompt = "一只可爱的小猫在花园里玩耍，阳光明媚，鲜花盛开，镜头缓慢推进";
                            Console.WriteLine("[测试] 使用文生视频模式");
                        }
                        
                        // 调用视频生成服务
                        var videoRequest = new VideoGenerationRequest
                        {
                            GenerationType = testImageInput != null ? VideoGenerationType.ImageToVideo : VideoGenerationType.TextToVideo,
                            Prompt = videoPrompt,
                            ImageInput = testImageInput,
                            Duration = 5,
                            Size = "720x1280",
                            AspectRatio = "9:16",
                            Resolution = "720P"  // Wan2.6需要大写的720P或1080P
                        };
                        
                        // 生成视频（提交任务并等待完成，最多等待300秒）
                        info = new Chats
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = "_userName",
                            AppId = "Test",
                            Context = "🎬 正在生成视频，请稍候（最多等待300秒）...",
                            CreateTime = DateTime.Now
                        };
                        MessageList.Add(info);
                        
                        var response = await _videoService.GenerateVideoAsync(LargeModel, videoRequest, maxWaitSeconds: 300);
                        
                        if (response.TaskStatus == VideoTaskStatus.Success && response.VideoUrls?.Count > 0)
                        {
                            var videoUrl = response.VideoUrls[0];
                            var testMode = testImageInput != null ? "图生视频" : "文生视频";
                            
                            info.Context = $"✅ 视频生成测试成功！\n\n模式：{testMode}\n提示词：{videoPrompt}\n\n生成的视频URL：\n{videoUrl}\n\n参数：时长={videoRequest.Duration}秒, 分辨率={videoRequest.Resolution}";
                        }
                        else
                        {
                            info.Context = $"⚠️ 视频生成未完成\n\n任务状态：{response.TaskStatus}\n任务ID：{response.TaskId ?? "无"}\n错误信息：{response.ErrorMessage ?? "无"}";
                        }
                    }
                    catch (Exception ex)
                    {
                        // 视频生成失败
                        info = new Chats
                        {
                            Id = Guid.NewGuid().ToString(),
                            UserName = "_userName",
                            AppId = "Test",
                            Context = $"❌ 视频生成测试失败！\n\n错误信息：{ex.Message}\n\n详细信息：{ex.StackTrace}",
                            CreateTime = DateTime.Now
                        };
                        
                        MessageList.Add(info);
                    }
                    break;
            }
            
            
            

            return JsonMsg<List<Chats>>.OK(MessageList);
        }
    }
}
