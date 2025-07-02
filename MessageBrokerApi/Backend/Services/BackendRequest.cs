using MessageBrokerApi.Backend.Interfaces;
using System.Net;

namespace MessageBrokerApi.Backend.Services
{
    public class BackendRequest : IBackendRequest
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<BackendRequest> _logger;

        public BackendRequest(IHttpClientFactory httpClientFactory, ILogger<BackendRequest> logger)
        {
            _httpClient = httpClientFactory.CreateClient();
            _logger = logger;
        }

        public async Task<HttpResponseMessage> DoRequest(string url, string method, string? content)
        {
            try
            {
                return method == "GET" ? await DoGetRequest(url) : await DoPostRequest(url, content);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "HttpRequestException occurred during request to {Url} with method {Method}", url, method);
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    ReasonPhrase = "HttpRequestException occurred"
                };
            }
        }

        private async Task<HttpResponseMessage> DoGetRequest(string url)
        {
            return await _httpClient.GetAsync(url);
        }

        private async Task<HttpResponseMessage> DoPostRequest(string url, string content)
        {
            var stringContent = new StringContent(content, System.Text.Encoding.UTF8, "application/json");
            return await _httpClient.PostAsync(url, stringContent);
        }
    }
}
