using NSubstitute;
using RagEvaluator.Application.Services;
using RagEvaluator.Contract.Abstractions.Services;

namespace RagEvaluator.Test.ApplicationTest
{
    public class HealthServiceTests
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IChatService _chatService;
        private readonly HealthService _service;

        public HealthServiceTests()
        {
            _embeddingService = Substitute.For<IEmbeddingService>();
            _chatService = Substitute.For<IChatService>();
            _service = new HealthService(_embeddingService, _chatService);
        }

        [Fact]
        public async Task IsReadyAsync_WhenBothServicesAvailable_ShouldReturnTrue()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);

            // Act
            var result = await _service.IsReadyAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsReadyAsync_WhenEmbeddingServiceUnavailable_ShouldReturnFalse()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(false);
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);

            // Act
            var result = await _service.IsReadyAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsReadyAsync_WhenChatServiceUnavailable_ShouldReturnFalse()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(false);

            // Act
            var result = await _service.IsReadyAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.False(result);
        }
    }
}
