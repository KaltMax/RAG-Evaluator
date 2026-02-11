using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using NSubstitute;
using RagEvaluator.API.Controllers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Test.ApiTest
{
    /// <summary>
    /// Provides unit tests for the ExperimentController class, verifying that its actions behave as expected under
    /// various scenarios.
    /// </summary>
    public class ExperimentControllerTests
    {
        private readonly ILogger<ExperimentController> _logger;
        private readonly IExperimentService _experimentService;
        private readonly ExperimentController _controller;

        public ExperimentControllerTests()
        {
            _logger = Substitute.For<ILogger<ExperimentController>>();
            _experimentService = Substitute.For<IExperimentService>();
            _controller = new ExperimentController(_logger, _experimentService);
        }

        #region CreateExperimentAsync Tests
        [Fact]
        public async Task CreateExperimentAsync_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateSampleExperimentRequest();
            request.Name = null!; // Invalid input to trigger model state error
            _controller.ModelState.AddModelError("Name", "The Name field is required.");
            
            // Act
            var result = await _controller.CreateExperimentAsync(request, CancellationToken.None);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("Name"));
        }

        [Fact]
        public async Task CreateExperimentAsync_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = CreateSampleExperimentRequest();
            var expectedResponse = CreateExperimentSummaryResponse();
            _experimentService.CreateExperimentAsync(request, Arg.Any<CancellationToken>())
                .Returns(expectedResponse);

            // Act
            var result = await _controller.CreateExperimentAsync(request, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<AcceptedResult>(result.Result);
            var actualResponse = Assert.IsType<ExperimentSummaryResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, actualResponse.Id);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ReturnsOkWithExperiments()
        {
            // Arrange
            var experiments = new List<ExperimentSummaryResponse>
            {
                CreateExperimentSummaryResponse(),
                CreateExperimentSummaryResponse()
            };
            _experimentService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(experiments);

            // Act
            var result = await _controller.GetAllAsync(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<ExperimentSummaryResponse>>(okResult.Value);
            Assert.Equal(2, response.Count());
        }

        [Fact]
        public async Task GetAllAsync_WhenNoExperiments_ReturnsEmptyList()
        {
            // Arrange
            _experimentService.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(new List<ExperimentSummaryResponse>());

            // Act
            var result = await _controller.GetAllAsync(CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<ExperimentSummaryResponse>>(okResult.Value);
            Assert.Empty(response);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithValidId_ReturnsOkWithExperiment()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            var experiment = CreateExperimentResponse(experimentId);
            _experimentService.GetByIdAsync(experimentId, Arg.Any<CancellationToken>()).Returns(experiment);

            // Act
            var result = await _controller.GetByIdAsync(experimentId, CancellationToken.None);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ExperimentResponse>(okResult.Value);
            Assert.Equal(experimentId, response.Id);
        }

        [Fact]
        public async Task GetByIdAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            _experimentService.GetByIdAsync(experimentId, Arg.Any<CancellationToken>())
                .Returns((ExperimentResponse?)null);

            // Act
            var result = await _controller.GetByIdAsync(experimentId, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            var experiment = CreateExperimentResponse(experimentId);
            _experimentService.GetByIdAsync(experimentId, Arg.Any<CancellationToken>()).Returns(experiment);

            // Act
            var result = await _controller.DeleteAsync(experimentId, CancellationToken.None);

            // Assert
            Assert.IsType<NoContentResult>(result);
            await _experimentService.Received(1).DeleteAsync(experimentId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            _experimentService.GetByIdAsync(experimentId, Arg.Any<CancellationToken>())
                .Returns((ExperimentResponse?)null);

            // Act
            var result = await _controller.DeleteAsync(experimentId, CancellationToken.None);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            await _experimentService.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region Helper Methods

        private CreateExperimentRequest CreateSampleExperimentRequest()
        {
            return new CreateExperimentRequest
            {
                Name = "Test Experiment",
                RepeatCount = 3,
                Queries = new List<ExperimentQueryItem>
                {
                    new ExperimentQueryItem
                    {
                        Question = "What is RAG?",
                        Language = "en",
                        TopK = 5
                    },
                    new ExperimentQueryItem
                    {
                        Question = "Was ist RAG?",
                        Language = "de",
                        TopK = 5
                    }
                }
            };
        }

        private ExperimentSummaryResponse CreateExperimentSummaryResponse(Guid? id = null)
        {
            return new ExperimentSummaryResponse
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Experiment",
                Status = "Completed",
                RepeatCount = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow,
                Progress = new ExperimentProgress { Total = 2, Completed = 2, Annotated = 0 },
                EmbeddingModel = "TestEmbeddingModel",
                ChunkingStrategy = "TestChunkingStrategy",
                PromptTemplate = "TestPromptTemplate"
            };
        }

        private ExperimentResponse CreateExperimentResponse(Guid? id = null)
        {
            return new ExperimentResponse
            {
                Id = id ?? Guid.NewGuid(),
                Name = "Test Experiment",
                Status = "Completed",
                RepeatCount = 3,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                CompletedAt = DateTime.UtcNow,
                Progress = new ExperimentProgress { Total = 2, Completed = 2, Annotated = 0 },
                EmbeddingModel = "TestEmbeddingModel",
                ChunkingStrategy = "TestChunkingStrategy",
                ChatModel = "TestChatModel",
                ChunkSize = 512,
                ChunkOverlap = 50,
                SimilarityThreshold = 0.7,
                PromptTemplate = "TestPromptTemplate"
            };
        }

        #endregion
    }
}
