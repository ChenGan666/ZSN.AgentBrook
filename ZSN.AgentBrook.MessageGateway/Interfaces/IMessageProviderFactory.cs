namespace ZSN.AgentBrook.MessageGateway.Interfaces
{
    public interface IMessageProviderFactory
    {
        IMessageProvider GetProvider(int providerType);

        Task<IMessageProvider> GetAvailableProviderAsync(int providerType, CancellationToken ct = default);

        void RecordFailure(int providerType);
        void RecordSuccess(int providerType);

        IEnumerable<int> GetRegisteredProviderTypes();
    }
}
