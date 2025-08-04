using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.Common.Messages;
using MessageBrokerApi.MessageQueue.Interfaces;
using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using System.Collections.Concurrent;
using System.Text;
using System.Text.Json;

namespace MessageBrokerApi.MessageQueue.Storages
{
    public sealed class RabbitMqMessageStorage : IMessageStorage
    {
        private readonly ILogger<RabbitMqMessageStorage> _logger;
        private readonly IConnection _connection;
        private readonly IChannel _channel;
        private readonly TimeSpan _timeout;
        private readonly string _requestQueue = "request_queue";
        private readonly string _responseQueue = "response_queue";
        private readonly ConcurrentDictionary<string, TaskCompletionSource<(int, string)>> _responses = new();

        public RabbitMqMessageStorage(IBrokerConfig config, ILogger<RabbitMqMessageStorage> logger)
        {
            var factory = new ConnectionFactory() { HostName = "localhost" };
            _connection = factory.CreateConnectionAsync().GetAwaiter().GetResult();
            _channel = _connection.CreateChannelAsync().GetAwaiter().GetResult();

            _channel.QueueDeclareAsync(_requestQueue, durable: false, exclusive: false, autoDelete: false);
            _channel.QueueDeclareAsync(_responseQueue, durable: false, exclusive: false, autoDelete: false);

            var consumer = new AsyncEventingBasicConsumer(_channel);
            consumer.ReceivedAsync += OnResponseReceivedAsync;
            _channel.BasicConsumeAsync(queue: _responseQueue, autoAck: true, consumer: consumer);
            _timeout = TimeSpan.FromSeconds(config.BrokerTimeoutSec);
            _logger = logger;
        }

        public async Task WriteRequestAsync(RequestMessage message)
        {
            var json = JsonSerializer.Serialize(message);
            var bodyBytes = Encoding.UTF8.GetBytes(json);

            var props = new BasicProperties();

            await _channel.BasicPublishAsync(
                exchange: "",
                routingKey: _requestQueue,
                mandatory: false,
                basicProperties: props,
                body: bodyBytes);
        }

        public async Task<(int StatusCode, string Body)> WaitForResponseAsync(string key)
        {
            var tcs = new TaskCompletionSource<(int, string)>(TaskCreationOptions.RunContinuationsAsynchronously);
            _responses[key] = tcs;

            using var cts = new CancellationTokenSource(_timeout);
            await using var _ = cts.Token.Register(() => tcs.TrySetCanceled(), useSynchronizationContext: false);

            return await tcs.Task;
        }

        public void CleanUp(string key)
        {
            _responses.TryRemove(key, out _);
        }

        private async Task OnResponseReceivedAsync(object sender, BasicDeliverEventArgs ea)
        {
            var bodyBytes = ea.Body.ToArray();
            var json = Encoding.UTF8.GetString(bodyBytes);

            try
            {
                var resp = JsonSerializer.Deserialize<ResponseMessage>(json);
                if (resp != null && _responses.TryRemove(resp.Key, out var tcs))
                {
                    tcs.TrySetResult((resp.StatusCode, resp.Body));
                }
                else
                {
                    _logger.LogWarning("Response with unknown or missing key received: {Json}", json);
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to deserialize response message: {Json}", json);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error during message processing: {Json}", json);
            }

            await Task.Yield();
        }

        public void Dispose()
        {
            _channel?.Dispose();
            _connection?.Dispose();
        }
    }
}
