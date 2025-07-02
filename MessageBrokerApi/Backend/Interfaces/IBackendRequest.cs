namespace MessageBrokerApi.Backend.Interfaces
{
    public interface IBackendRequest
    {
        Task<HttpResponseMessage> DoRequest(string url, string method, string? content);
    }
}
