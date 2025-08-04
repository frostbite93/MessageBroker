using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.MessageQueue.Interfaces;
using MessageBrokerApi.MessageQueue.Storages;

namespace MessageBrokerApi.MessageQueue.Factories
{
    public sealed class MessageStorageFactory : IMessageStorageFactory
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IBrokerConfig _config;

        public MessageStorageFactory(IServiceProvider serviceProvider, IBrokerConfig config)
        {
            _serviceProvider = serviceProvider;
            _config = config;
        }

        public IMessageStorage Create()
        {
            switch (_config.BrokerStorageType)
            {
                case "RabbitMq":
                    return new RabbitMqMessageStorage(_config, _serviceProvider.GetRequiredService<ILogger<RabbitMqMessageStorage>>());
                case "File":
                    return new FileMessageStorage(_config, _serviceProvider.GetRequiredService<ILogger<FileMessageStorage>>());
                default:
                    return new FileMessageStorage(_config, _serviceProvider.GetRequiredService<ILogger<FileMessageStorage>>());
            }
        }
    }
}
