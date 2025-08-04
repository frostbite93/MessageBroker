namespace MessageBrokerApi.Common.Configuration
{
    public class BrokerConfig : IBrokerConfig
    {
        private readonly IConfiguration _config;

        public BrokerConfig(IConfiguration config)
        {
            _config = config;
        }

        public string BrokerDirectory => _config.GetValue<string>("Broker:Directory") ?? "C:\\temp\\broker";
        public string BrokerIncorrectFilesDirectory => _config.GetValue<string>("Broker:IncorrectFilesDirectory") ?? "C:\\temp\\broker\\incorrect";
        public int BrokerFileAgeThresholdMin => _config.GetValue("Broker:FileAgeThresholdMin", 5);
        public bool BrokerAdvancedMode => _config.GetValue<bool>("Broker:AdvancedMode");
        public int BrokerTimeoutSec => _config.GetValue("Broker:TimeoutSec", 90);
        public int BrokerResponseCacheLifetimeSeconds => _config.GetValue("Broker:ResponseCacheLifetimeSeconds", 30);
        public string BrokerBackendUrl => _config.GetValue<string>("Broker:BackendUrl") ?? "https://localhost:64172";
    }
}
