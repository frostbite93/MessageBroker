namespace MessageBrokerApi.MessageQueue.Interfaces
{
    public interface IMessageBroker
    {
        Task<(int StatusCode, string Body)> SendAndWaitAsync(string method, string path, string body);
    }
}
