namespace MessageBrokerApi.Common.Configuration
{
    public interface IBrokerConfig
    {
        string BrokerDirectory { get; }
        string BrokerIncorrectFilesDirectory { get; }
        int BrokerFileAgeThresholdMin { get; }
        bool BrokerAdvancedMode { get; }
        int BrokerTimeoutSec { get; }
        int BrokerResponseCacheLifetimeSeconds { get; }
        string BrokerBackendUrl { get; }
    }
}
