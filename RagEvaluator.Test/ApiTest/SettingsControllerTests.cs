using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RagEvaluator.API.Controllers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Test.ApiTest
{
    public class SettingsControllerTests
    {
        private readonly ILogger<SettingsController> _logger;
        private readonly ISettingsService _settingsService;
        private readonly SettingsController _controller;

        public SettingsControllerTests()
        {
            _logger = Substitute.For<ILogger<SettingsController>>();
            _settingsService = Substitute.For<ISettingsService>();
            _controller = new SettingsController(_logger, _settingsService);
        }

        #region GetSettings Tests

        [Fact]
        public void GetSettings_ReturnsOkWithSettings()
        {
            // Arrange
            var expectedResponse = CreateSampleSettingsResponse();
            _settingsService.GetSettings().Returns(expectedResponse);

            // Act
            var result = _controller.GetSettings();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResponse = Assert.IsType<SettingsResponse>(okResult.Value);
            Assert.Equal(expectedResponse, actualResponse);
        }

        #endregion

        #region UpdateSettings Tests
        [Fact]
        public async Task UpdateSettingsAsync_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateSampleUpdateSettingsRequest();
            request.ChunkSize = -1; // Invalid chunk size
            _controller.ModelState.AddModelError("ChunkSize", "ChunkSize must be a positive integer.");

            // Act
            var result = await _controller.UpdateSettings(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("ChunkSize"));
        }

        [Fact]
        public async Task UpdateSettingsAsync_WithValidRequest_ReturnsOkWithUpdatedSettings()
        {
            // Arrange
            var request = CreateSampleUpdateSettingsRequest();
            var expectedResponse = CreateSampleSettingsResponse();
            _settingsService.UpdateSettingsAsync(request).Returns(expectedResponse);

            // Act
            var result = await _controller.UpdateSettings(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResponse = Assert.IsType<SettingsResponse>(okResult.Value);
            Assert.Equal(expectedResponse, actualResponse);
        }

        #endregion

        #region Helper Methods

        private UpdateSettingsRequest CreateSampleUpdateSettingsRequest()
        {
            return new UpdateSettingsRequest
            {
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChunkingStrategy = Domain.Enums.ChunkingStrategy.FixedSize,
                PromptTemplate = Domain.Enums.PromptTemplate.Basic,
                ChunkSize = 1000,
                ChunkOverlap = 200,
                SimilarityThreshold = 0.6,
            };
        }

        private SettingsResponse CreateSampleSettingsResponse()
        {
            return new SettingsResponse
            {
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChunkingStrategy = Domain.Enums.ChunkingStrategy.FixedSize,
                PromptTemplate = Domain.Enums.PromptTemplate.Basic,
                ChunkSize = 1000,
                ChunkOverlap = 200,
                SimilarityThreshold = 0.6,
                AvailableEmbeddingModels = new List<string> 
                { 
                    "nomic-embed-text-v2-moe", 
                    "text-embedding-3-small" 
                },
                AvailableChunkingStrategies = new List<Domain.Enums.ChunkingStrategy> 
                { 
                    Domain.Enums.ChunkingStrategy.FixedSize, 
                    Domain.Enums.ChunkingStrategy.Semantic 
                },
                PromptBasicText = "This is a basic prompt template.",
                PromptInstructedText = "This is an instructed prompt template.",
                PromptLanguageAwareEnText = "This is a language-aware prompt template for English.",
                PromptLanguageAwareDeText = "This is a language-aware prompt template for German."
            };

        }

        #endregion
    }
}
