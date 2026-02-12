using NSubstitute;
using RagEvaluator.Application.Services;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class SettingsServiceTests
    {
        private readonly RagConfiguration _config;
        private readonly IEmbeddingService _embeddingService;
        private readonly SettingsService _service;

        public SettingsServiceTests()
        {
            _config = CreateSampleRagConfiguration();
            _embeddingService = Substitute.For<IEmbeddingService>();
            _service = new SettingsService(_config, _embeddingService);
        }

        #region GetSettings Tests

        [Fact]
        public void GetSettings_ShouldReturnCurrentConfiguration()
        {
            // Act
            var result = _service.GetSettings();

            // Assert
            Assert.Equal(_config.EmbeddingModel, result.EmbeddingModel);
            Assert.Equal(_config.ChunkingStrategy, result.ChunkingStrategy);
            Assert.Equal(_config.PromptTemplate, result.PromptTemplate);
            Assert.Equal(_config.ChunkSize, result.ChunkSize);
            Assert.Equal(_config.ChunkOverlap, result.ChunkOverlap);
            Assert.Equal(_config.SimilarityThreshold, result.SimilarityThreshold);
            Assert.Equal(_config.PromptBasic, result.PromptBasicText);
            Assert.Contains("nomic-embed-text-v2-moe", result.AvailableEmbeddingModels);
        }

        #endregion

        #region UpdateSettingsAsync Tests

        [Fact]
        public async Task UpdateSettingsAsync_WithValidPartialUpdate_ShouldOnlyUpdateProvidedFields()
        {
            // Arrange
            var request = new UpdateSettingsRequest { ChunkSize = 500 };

            // Act
            var result = await _service.UpdateSettingsAsync(request);

            // Assert
            Assert.Equal(500, result.ChunkSize);
            Assert.Equal(200, result.ChunkOverlap); // unchanged
            Assert.Equal("nomic-embed-text-v2-moe", result.EmbeddingModel); // unchanged
        }

        [Fact]
        public async Task UpdateSettingsAsync_WithAllFields_ShouldUpdateAllFields()
        {
            // Arrange
            var request = new UpdateSettingsRequest
            {
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChunkingStrategy = ChunkingStrategy.Semantic,
                PromptTemplate = PromptTemplate.Instructed,
                ChunkSize = 2000,
                ChunkOverlap = 400,
                SimilarityThreshold = 0.8
            };

            // Act
            var result = await _service.UpdateSettingsAsync(request);

            // Assert
            Assert.Equal("nomic-embed-text-v2-moe", result.EmbeddingModel);
            Assert.Equal(ChunkingStrategy.Semantic, result.ChunkingStrategy);
            Assert.Equal(PromptTemplate.Instructed, result.PromptTemplate);
            Assert.Equal(2000, result.ChunkSize);
            Assert.Equal(400, result.ChunkOverlap);
            Assert.Equal(0.8, result.SimilarityThreshold);
        }

        [Fact]
        public async Task UpdateSettingsAsync_WithInvalidEmbeddingModel_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new UpdateSettingsRequest { EmbeddingModel = "nonexistent-model" };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(request));
        }

        [Fact]
        public async Task UpdateSettingsAsync_WithOverlapGreaterThanChunkSize_ShouldThrowArgumentException()
        {
            // Arrange — overlap 500 >= effective chunk size 200
            var request = new UpdateSettingsRequest { ChunkSize = 200, ChunkOverlap = 500 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(request));
        }

        [Fact]
        public async Task UpdateSettingsAsync_WithOverlapEqualToChunkSize_ShouldThrowArgumentException()
        {
            // Arrange
            var request = new UpdateSettingsRequest { ChunkSize = 500, ChunkOverlap = 500 };

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() => _service.UpdateSettingsAsync(request));
        }

        [Fact]
        public async Task UpdateSettingsAsync_WhenEmbeddingModelChanges_ShouldReinitializeEmbeddingService()
        {
            // Arrange — config has a second available model
            _config.AvailableEmbeddingModels = "nomic-embed-text-v2-moe,all-minilm";
            var request = new UpdateSettingsRequest { EmbeddingModel = "all-minilm" };

            // Act
            await _service.UpdateSettingsAsync(request);

            // Assert
            await _embeddingService.Received(1).ReinitializeAsync();
        }

        [Fact]
        public async Task UpdateSettingsAsync_WhenEmbeddingModelUnchanged_ShouldNotReinitialize()
        {
            // Arrange — same model as current config
            var request = new UpdateSettingsRequest { EmbeddingModel = "nomic-embed-text-v2-moe" };

            // Act
            await _service.UpdateSettingsAsync(request);

            // Assert
            await _embeddingService.DidNotReceive().ReinitializeAsync();
        }

        #endregion

        #region Helper Methods

        private RagConfiguration CreateSampleRagConfiguration()
        {
            return new RagConfiguration
            {
                OllamaEndpoint = "http://localhost:11434/v1",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                AvailableEmbeddingModels = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                ChunkingStrategy = ChunkingStrategy.FixedSize,
                PromptTemplate = PromptTemplate.Basic,
                PromptBasic = "You are a helpful assistant.",
                PromptInstructed = "You are a helpful assistant.",
                PromptLanguageAwareEn = "You are a helpful assistant.",
                PromptLanguageAwareDe = "Du bist ein hilfreicher Assistent.",
                ChunkSize = 1000,
                ChunkOverlap = 200,
                SimilarityThreshold = 0.5
            };
        }

        #endregion
    }
}
