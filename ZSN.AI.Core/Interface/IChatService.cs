﻿
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ZSN.AI.Entity;

namespace ZSN.AI.Core.Interface
{
    public interface IChatService
    {
        IAsyncEnumerable<string> SendChatAsync(LargeModelConfig Model, ChatHistory history, CallFunction? Function = null);
        IAsyncEnumerable<string> SendKmsAsync(List<KnowledgeBaseUnit> KnowledgeBaseUnits, LargeModelConfig ChatModel, string questions, ChatHistory history);
        //IAsyncEnumerable<StreamingKernelContent> SendKmsAsync(LargeModelUnit Model, string questions, ChatHistory history, string filePath, List<RelevantSource> relevantSources = null);
        //Task<string> SendImgAsync(LargeModelConfig Model, string questions);
        ChatHistory GetChatHistory(List<AppChatLogInfo> MessageList, ChatHistory history);
        ChatHistory GetChatHistory(List<AppChatSummaryInfo> MessageList, ChatHistory history);
        IAsyncEnumerable<string> HistorySummarize(LargeModelConfig ModelConfig, ChatHistory history);
        IAsyncEnumerable<string> FunctionCall(LargeModelConfig ModelConfig, CallFunction callFunction, KernelArguments keyValuePairs = null);
        IAsyncEnumerable<string> PromptFunctionCall(LargeModelConfig ModelConfig, CallFunction callFunction, KernelArguments keyValuePairs = null);
    }
}