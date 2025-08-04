using MessageBrokerApi.Common.Messages;

namespace MessageBrokerApi.MessageQueue.Interfaces
{
    public interface IMessageStorage
    {
        Task WriteRequestAsync(RequestMessage message);
        Task<(int StatusCode, string Body)> WaitForResponseAsync(string key);
        void CleanUp(string key);
    }
}