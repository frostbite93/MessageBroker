using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.Common.Hashing;
using MessageBrokerApi.Common.Messages;
using MessageBrokerApi.MessageQueue.Interfaces;
using MessageBrokerApi.MessageQueue.Services;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;

namespace Tests.MessageQueue
{
    public class MessageBrokerTests
    {
        private readonly MD5HashGenerator _hashGen;

        private readonly Mock<IBrokerConfig> _mockConfig;
        private readonly Mock<IMessageStorage> _mockStorage;
        private readonly Mock<ILogger<MessageBroker>> _mockLogger;
        private readonly Mock<IMemoryCache> _mockCache;
        private readonly Mock<ICacheEntry> _mockCacheEntry;
        private readonly MessageBroker _broker;

        public MessageBrokerTests()
        {
            _mockConfig = new Mock<IBrokerConfig>();
            _mockConfig.SetupGet(c => c.BrokerAdvancedMode).Returns(true);
            _mockConfig.SetupGet(c => c.BrokerResponseCacheLifetimeSeconds).Returns(30);

            _hashGen = new MD5HashGenerator();
            _mockStorage = new Mock<IMessageStorage>();
            _mockLogger = new Mock<ILogger<MessageBroker>>();
            _mockCache = new Mock<IMemoryCache>();
            _mockCacheEntry = new Mock<ICacheEntry>();

            _mockCache.Setup(x => x.CreateEntry(It.IsAny<object>())).Returns(_mockCacheEntry.Object);

            _broker = new MessageBroker(_mockConfig.Object, _hashGen, _mockLogger.Object, _mockStorage.Object, _mockCache.Object);
        }

        [Fact]
        public async Task SendAndWaitAsync_ShouldTimeout_IfNoResponseAppears()
        {
            // Arrange
            var method = "POST";
            var path = "/timeout";
            var body = "";

            object dummy;
            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out dummy)).Returns(false);
            _mockStorage.Setup(x => x.WriteRequestAsync(It.IsAny<RequestMessage>())).Returns(Task.CompletedTask);
            _mockStorage.Setup(x => x.WaitForResponseAsync(It.IsAny<string>())).ThrowsAsync(new TimeoutException("Response not received in time."));

            // Act & Assert
            await Assert.ThrowsAsync<TimeoutException>(() =>
                _broker.SendAndWaitAsync(method, path, body));
        }

        [Fact]
        public async Task SendAndWaitAsync_ShouldReturn500_WhenResponseFileIsInvalid()
        {
            // Arrange
            var method = "GET";
            var path = "/invalid";
            var body = "";
            var key = "invalid-key";

            object dummy;
            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out dummy)).Returns(false);
            _mockStorage.Setup(x => x.WriteRequestAsync(It.IsAny<RequestMessage>())).Returns(Task.CompletedTask);
            _mockStorage.Setup(x => x.WaitForResponseAsync(It.IsAny<string>())).ReturnsAsync((500, "Invalid response format"));

            // Act
            var result = await _broker.SendAndWaitAsync(method, path, body);

            // Assert
            Assert.Equal(500, result.StatusCode);
            Assert.Contains("Invalid", result.Body);
        }

        [Fact]
        public async Task SendAndWaitAsync_ShouldWriteRequestOnce()
        {
            // Arrange
            var method = "POST";
            var path = "/write-once";
            var body = "some-body";
            var key = _hashGen.ComputeHash(method + path + body);

            object dummy;
            _mockCache.Setup(x => x.TryGetValue(It.IsAny<object>(), out dummy)).Returns(false);
            _mockStorage.Setup(x => x.WriteRequestAsync(It.IsAny<RequestMessage>())).Returns(Task.CompletedTask);

            // Act
            try
            {
                await _broker.SendAndWaitAsync(method, path, body);
            }
            catch (TimeoutException) { }

            // Assert
            _mockStorage.Verify(x => x.WriteRequestAsync(It.IsAny<RequestMessage>()), Times.Once);
        }
    }
}
