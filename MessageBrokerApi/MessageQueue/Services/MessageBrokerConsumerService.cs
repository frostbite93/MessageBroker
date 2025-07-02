using MessageBrokerApi.Backend.Interfaces;
using MessageBrokerApi.MessageQueue.Interfaces;

namespace MessageBrokerApi.MessageQueue.Services
{
    public class MessageBrokerConsumerService(ILogger<MessageBrokerConsumerService> logger, IBackendRequest backendRequest, IMessageStorageProvider storageProvider) : BackgroundService
    {
        private readonly ILogger<MessageBrokerConsumerService> _logger = logger;
        private readonly IBackendRequest _backendRequest = backendRequest;
        private readonly IMessageStorageProvider _storageProvider = storageProvider;

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("MessageBrokerConsumerService started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    var requestNames = await _storageProvider.GetRequestsAsync();

                    foreach (var requestName in requestNames)
                    {
                        var (method, path, body) = await _storageProvider.ReadRequestAsync(requestName);
                        if (method == null || path == null)
                        {
                            continue;
                        }

                        var response = await _backendRequest.DoRequest(path, method, body);
                        if (response == null)
                        {
                            _logger.LogError($"Response on request {requestName} is null");
                            continue;
                        }

                        await _storageProvider.WriteResponseAsync(requestName, response);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Top-level error in consumer loop");
                }

                await Task.Delay(500, stoppingToken);
            }

            _logger.LogInformation("MessageBrokerConsumerService stopped.");
        }
    }
}
