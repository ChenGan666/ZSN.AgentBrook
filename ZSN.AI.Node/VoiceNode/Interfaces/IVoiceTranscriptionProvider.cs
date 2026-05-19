using ZSN.AI.Node.VoiceNode.Models;

namespace ZSN.AI.Node.VoiceNode.Interfaces
{
    /// <summary>
    /// 语音转写 Provider 抽象接口
    /// </summary>
    public interface IVoiceTranscriptionProvider
    {
        /// <summary>Provider 名称标识</summary>
        string ProviderName { get; }

        /// <summary>文件转写</summary>
        Task<TranscriptionResult> TranscribeAsync(
            TranscribeRequest request,
            IProgress<VoiceProgress> progress,
            CancellationToken cancellationToken);

        /// <summary>健康检查</summary>
        Task<bool> IsHealthyAsync(CancellationToken cancellationToken = default);

        /// <summary>是否支持指定功能</summary>
        bool SupportsFeature(VoiceFeature feature);
    }
}
