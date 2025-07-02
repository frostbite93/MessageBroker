using System.Net;
using MessageBrokerApi.Backend.Services;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;

namespace Tests.Backend
{
    public class BackendRequestTests
    {
        private readonly Mock<IHttpClientFactory> _httpClientFactoryMock;
        private readonly Mock<ILogger<BackendRequest>> _loggerMock;

        public BackendRequestTests()
        {
            _httpClientFactoryMock = new Mock<IHttpClientFactory>();
            _loggerMock = new Mock<ILogger<BackendRequest>>();
        }

        private BackendRequest CreateBackendRequest(HttpMessageHandler handler)
        {
            var httpClient = new HttpClient(handler);
            _httpClientFactoryMock.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(httpClient);

            return new BackendRequest(_httpClientFactoryMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task DoRequest_ShouldReturnResponse_ForGet()
        {
            // Arrange
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("GET response")
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(expectedResponse);

            var backendRequest = CreateBackendRequest(handlerMock.Object);

            // Act
            var response = await backendRequest.DoRequest("http://test.com", "GET", null);

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("GET response", body);
        }

        [Fact]
        public async Task DoRequest_ShouldReturnResponse_ForPost()
        {
            // Arrange
            var expectedResponse = new HttpResponseMessage(HttpStatusCode.Created)
            {
                Content = new StringContent("POST response")
            };

            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.Is<HttpRequestMessage>(req =>
                        req.Method == HttpMethod.Post &&
                        req.Content.ReadAsStringAsync().Result == "{\"foo\":\"bar\"}"),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ReturnsAsync(expectedResponse);

            var backendRequest = CreateBackendRequest(handlerMock.Object);

            // Act
            var response = await backendRequest.DoRequest("http://test.com", "POST", "{\"foo\":\"bar\"}");

            // Assert
            Assert.Equal(HttpStatusCode.Created, response.StatusCode);
            var body = await response.Content.ReadAsStringAsync();
            Assert.Equal("POST response", body);
        }

        [Fact]
        public async Task DoRequest_ShouldReturnInternalServerError_OnHttpRequestException()
        {
            // Arrange
            var handlerMock = new Mock<HttpMessageHandler>();
            handlerMock.Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>()
                )
                .ThrowsAsync(new HttpRequestException("Test exception"));

            var backendRequest = CreateBackendRequest(handlerMock.Object);

            // Act
            var response = await backendRequest.DoRequest("http://test.com", "GET", null);

            // Assert
            Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
            Assert.Equal("HttpRequestException occurred", response.ReasonPhrase);

            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("HttpRequestException")),
                    It.IsAny<HttpRequestException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}