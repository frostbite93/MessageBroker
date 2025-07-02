using Moq;
using Microsoft.Extensions.Logging;
using MessageBrokerApi.MessageQueue.Services;
using MessageBrokerApi.Common.Hashing;
using MessageBrokerApi.Common.Configuration;
using MessageBrokerApi.MessageQueue.Interfaces;

namespace Tests.MessageQueue
{
    public class MessageBrokerTests
    {
        private readonly MD5HashGenerator _hashGen;

        private readonly Mock<IBrokerConfig> _mockConfig;
        private readonly Mock<IMessageStorage> _mockStorage;
        private readonly Mock<ILogger<MessageBroker>> _mockLogger;
        private readonly MessageBroker _broker;

        public MessageBrokerTests()
        {
            _mockConfig = new Mock<IBrokerConfig>();
            _mockConfig.SetupGet(c => c.BrokerAdvancedMode).Returns(true);

            _hashGen = new MD5HashGenerator();

            _mockLogger = new Mock<ILogger<MessageBroker>>();

            //_mockStorageProvider = new Mock<IMessageStorageProvider>();
            _mockStorage = new Mock<IMessageStorage>();
            _mockLogger = new Mock<ILogger<MessageBroker>>();

            _broker = new MessageBroker(_mockConfig.Object, _hashGen, _mockLogger.Object, _mockStorage.Object);
        }

        [Fact]
        public async Task SendAndWaitAsync_ShouldTimeout_IfNoResponseAppears()
        {
            // Arrange
            var method = "POST";
            var path = "/timeout";
            var body = "";

            _mockStorage.Setup(x => x.WriteRequestAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
                .Returns(Task.CompletedTask);

            _mockStorage.Setup(x => x.WaitForResponseAsync(It.IsAny<string>()))
               .ThrowsAsync(new TimeoutException("Response not received in time."));

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

            _mockStorage.Setup(x => x.WriteRequestAsync(key, method, path, body))
                .Returns(Task.CompletedTask);

            _mockStorage.Setup(x => x.WaitForResponseAsync(It.IsAny<string>()))
                .ReturnsAsync((500, "Invalid response format"));

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

            _mockStorage.Setup(x => x.WriteRequestAsync(key, method, path, body)).Returns(Task.CompletedTask);

            // Act
            try
            {
                await _broker.SendAndWaitAsync(method, path, body);
            }
            catch (TimeoutException) { }

            // Assert
            _mockStorage.Verify(x => x.WriteRequestAsync(key, method, path, body), Times.Once);
        }
    }
}
