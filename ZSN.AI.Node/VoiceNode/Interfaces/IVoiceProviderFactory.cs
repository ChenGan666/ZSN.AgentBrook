namespace ZSN.AI.Node.VoiceNode.Interfaces
{
    /// <summary>
    /// Provider 工厂接口
    /// </summary>
    public interface IVoiceProviderFactory
    {
        /// <summary>根据名称获取 Provider 实例</summary>
        IVoiceTranscriptionProvider GetProvider(string providerName);

        /// <summary>获取可用的 Provider（含健康检查 + 降级逻辑）</summary>
        Task<IVoiceTranscriptionProvider> GetAvailableProviderAsync(
            string preferredProvider,
            CancellationToken cancellationToken = default);

        /// <summary>记录 Provider 调用失败</summary>
        void RecordFailure(string providerName);

        /// <summary>记录 Provider 调用成功</summary>
        void RecordSuccess(string providerName);
    }
}
