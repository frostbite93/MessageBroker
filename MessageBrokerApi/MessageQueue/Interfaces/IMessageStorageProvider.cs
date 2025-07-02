namespace MessageBrokerApi.MessageQueue.Interfaces
{
    public interface IMessageStorageProvider
    {
        Task<IEnumerable<string>> GetRequestsAsync();
        Task<(string? Method, string? Path, string? Body)> ReadRequestAsync(string req);
        Task WriteResponseAsync(string req, HttpResponseMessage response);
    }
}