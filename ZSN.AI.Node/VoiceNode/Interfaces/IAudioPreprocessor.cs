using ZSN.AI.Node.VoiceNode.Models;

namespace ZSN.AI.Node.VoiceNode.Interfaces
{
    /// <summary>
    /// 音频预处理器接口
    /// </summary>
    public interface IAudioPreprocessor
    {
        /// <summary>预处理音频文件</summary>
        Task<AudioPreprocessResult> PreprocessAsync(
            string inputPath,
            AudioPreprocessOptions options,
            CancellationToken cancellationToken = default);
    }
}
