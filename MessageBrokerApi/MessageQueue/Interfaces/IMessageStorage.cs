namespace MessageBrokerApi.MessageQueue.Interfaces
{
    public interface IMessageStorage
    {
        Task WriteRequestAsync(string key, string method, string path, string body);
        Task<(int StatusCode, string Body)> WaitForResponseAsync(string key);
        void CleanUp(string key);
    }
}