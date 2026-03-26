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
    /// Provides unit tests for the QueryController class, verifying the correctness of its API endpoints under various
    /// scenarios.
    /// </summary>
    public class QueryControllerTests
    {
        private readonly ILogger<QueryController> _logger;
        private readonly IRagService _ragService;
        private readonly IQueryService _queryService;
        private readonly QueryController _controller;

        public QueryControllerTests()
        {
            _logger = Substitute.For<ILogger<QueryController>>();
            _ragService = Substitute.For<IRagService>();
            _queryService = Substitute.For<IQueryService>();
            _controller = new QueryController(_logger, _ragService, _queryService);
        }

        #region QueryAsync Tests

        [Fact]
        public async Task QueryAsync_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateSampleQueryRequest();
            request.Question = null!; // Invalid input to trigger model state error
            _controller.ModelState.AddModelError("Question", "The Question field is required.");

            // Act
            var result = await _controller.QueryAsync(request, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("Question"));
        }

        [Fact]
        public async Task QueryAsync_WithValidRequest_ReturnsOk()
        {
            // Arrange
            var request = CreateSampleQueryRequest();
            var expectedResponse = CreateSampleQueryResponse();
            _ragService.AskQuestionAsync(request, Arg.Any<CancellationToken>())
                .Returns(expectedResponse);

            // Act
            var result = await _controller.QueryAsync(request, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var actualResponse = Assert.IsType<QueryResponse>(okResult.Value);
            Assert.Equal(expectedResponse, actualResponse);
        }

        #endregion

        #region GetQueryHistoryAsync Tests

        [Fact]
        public async Task GetQueryHistoryAsync_ReturnsOkWithQueryHistory()
        {
            // Arrange
            var queryHistory = new List<QuerySummaryResponse>
            {
                CreateSampleQuerySummaryResponse(),
                CreateSampleQuerySummaryResponse()
            };
            _queryService.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(queryHistory);

            // Act
            var result = await _controller.GetQueryHistoryAsync(TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<List<QuerySummaryResponse>>(okResult.Value);
            Assert.Equal(queryHistory, response);
        }

        [Fact]
        public async Task GetQueryHistoryAsync_WhenNoQuery_ReturnsOkWithEmptyList()
        {
            // Arrange
            var queryHistory = new List<QuerySummaryResponse>();
            _queryService.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(queryHistory);

            // Act
            var result = await _controller.GetQueryHistoryAsync(TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<List<QuerySummaryResponse>>(okResult.Value);
            Assert.Empty(response);
        }

        #endregion

        #region GetQueryByIdAsync Tests

        [Fact]
        public async Task GetQueryById_WithValidId_ReturnsOkWithQuery()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            var query = CreateSampleQueryResponse(queryId);
            _queryService.GetByIdAsync(queryId, Arg.Any<CancellationToken>())
                .Returns(query);

            // Act
            var result = await _controller.GetQueryByIdAsync(queryId, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<QueryResponse>(okResult.Value);
            Assert.Equal(query, response);
        }

        [Fact]
        public async Task GetQueryById_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            _queryService.GetByIdAsync(queryId, Arg.Any<CancellationToken>())
                .Returns((QueryResponse?)null);

            // Act
            var result = await _controller.GetQueryByIdAsync(queryId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        #endregion

        #region AnnotateResultsAsync Tests

        [Fact]
        public async Task AnnotateResultsAsync_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var request = CreateSampleAnnotateResultsRequest();
            request.Annotations = null!; // Invalid input to trigger model state error
            _controller.ModelState.AddModelError("Annotations", "The Annotations field is required.");

            // Act
            var result = await _controller.AnnotateResultsAsync(Guid.NewGuid(), request, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var modelState = Assert.IsType<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("Annotations"));
        }

        [Fact]
        public async Task AnnotateResultsAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            _queryService.GetByIdAsync(queryId, Arg.Any<CancellationToken>())
                .Returns((QueryResponse?)null);

            // Act
            var result = await _controller.AnnotateResultsAsync(queryId, CreateSampleAnnotateResultsRequest(), TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        [Fact]
        public async Task AnnotateQueryResultAsync_WithValidRequest_ReturnsOkWithUpdatedQuery()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            var existingQuery = CreateSampleQueryResponse(queryId);
            _queryService.GetByIdAsync(queryId, Arg.Any<CancellationToken>())
                .Returns(existingQuery);

            // Act
            var result = await _controller.AnnotateResultsAsync(queryId, CreateSampleAnnotateResultsRequest(), TestContext.Current.CancellationToken);
            
            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<QueryResponse>(okResult.Value);
            Assert.Equal(existingQuery, response);
            await _queryService.Received(1).AnnotateResultsAsync(
                queryId,
                Arg.Any<IEnumerable<QueryResultAnnotation>>(),
                Arg.Any<Domain.Enums.ResponseQuality>(),
                Arg.Any<bool>(),
                Arg.Any<IEnumerable<Guid>>(),
                Arg.Any<CancellationToken>());
        }

        #endregion

        #region DeleteQueryAsync Tests

        [Fact]
        public async Task DeleteQueryAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            _queryService.GetByIdAsync(queryId, Arg.Any<CancellationToken>())
                .Returns((QueryResponse?)null);

            // Act
            var result = await _controller.DeleteQueryAsync(queryId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            await _queryService.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteQueryAsync_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            var existingQuery = CreateSampleQueryResponse(queryId);
            _queryService.GetByIdAsync(queryId, Arg.Any<CancellationToken>())
                .Returns(existingQuery);

            // Act
            var result = await _controller.DeleteQueryAsync(queryId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NoContentResult>(result);
            await _queryService.Received(1).DeleteAsync(queryId, Arg.Any<CancellationToken>());
        }

        #endregion

        #region Helper Methods

        private AskQuestionRequest CreateSampleQueryRequest()
        {
            return new AskQuestionRequest
            {
                Question = "What is Serverless Computing?",
                Language = "en",
                TopK = 5,
            };
        }

        private QueryResponse CreateSampleQueryResponse(Guid? id = null)
        {
            return new QueryResponse
            {
                QueryId = id ?? Guid.NewGuid(),
                Question = "What is Serverless Computing?",
                Language = "en",
                TopK = 5,
                SystemPrompt = "Answer the question based on the retrieved documents.",
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                Answer = "Serverless computing is a cloud computing execution model where the cloud provider dynamically manages the allocation of machine resources. In this model, developers can focus on writing code without worrying about the underlying infrastructure, as the provider automatically handles scaling, load balancing, and server management.",
                Sources = new List<SearchResultDto>
                {
                    new SearchResultDto
                    {
                        Id = Guid.NewGuid(),
                        Text = "Serverless computing allows developers to build and run applications without having to manage servers. It abstracts away the infrastructure, enabling developers to focus on writing code.",
                        Similarity = 0.95,
                        DocumentId = Guid.NewGuid(),
                        FileName = "serverless_computing.pdf",
                        ChunkingStrategy = "FixedSize",
                        EmbeddingModel = "nomic-embed-text-v2-moe"

                    },
                    new SearchResultDto
                    {
                        Id = Guid.NewGuid(),
                        Text = "In serverless computing, the cloud provider automatically provisions, scales, and manages the infrastructure required to run applications. This allows developers to focus on writing code and building features without worrying about server management.",
                        Similarity = 0.92,
                        DocumentId = Guid.NewGuid(),
                        FileName = "cloud_computing_overview.pdf",
                        ChunkingStrategy = "FixedSize",
                        EmbeddingModel = "nomic-embed-text-v2-moe"
                    }
                },
                Timestamp = DateTime.UtcNow,
                ResponseTimeMs = 1500,
                Mrr = 1.0,
                PrecisionAtK = 1.0,
                RecallAtK = 1.0,
                NdcgAtK = 1.0,
                ResponseQuality = 5,
                HasLanguageSwitching = false
            };
        }

        private QuerySummaryResponse CreateSampleQuerySummaryResponse()
        {
            return new QuerySummaryResponse
            {
                Id = Guid.NewGuid(),
                Question = "What is Serverless Computing?",
                Language = "en",
                TopK = 5,
                SystemPrompt = "Answer the question based on the retrieved documents.",
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                Answer = "Serverless computing is a cloud computing execution model where the cloud provider dynamically manages the allocation of machine resources. In this model, developers can focus on writing code without worrying about the underlying infrastructure, as the provider automatically handles scaling, load balancing, and server management.",
                CreatedAt = DateTime.UtcNow,
                ResponseTimeMs = 1500,
                Mrr = 1.0,
                PrecisionAtK = 1.0,
                RecallAtK = 1.0,
                NdcgAtK = 1.0,
                ResponseQuality = 5,
                HasLanguageSwitching = false
            };
        }

        private AnnotateResultsRequest CreateSampleAnnotateResultsRequest()
        {
            return new AnnotateResultsRequest
            {
                Annotations = new List<QueryResultAnnotation>
                {
                    new QueryResultAnnotation
                    {
                        ResultId = Guid.NewGuid(),
                        RelevanceGrade = Domain.Enums.RelevanceGrade.Relevant
                    },
                    new QueryResultAnnotation
                    {
                        ResultId = Guid.NewGuid(),
                        RelevanceGrade = Domain.Enums.RelevanceGrade.NotRelevant
                    }
                },
                ResponseQuality = Domain.Enums.ResponseQuality.CorrectAndComplete,
                HasLanguageSwitching = false,
                RelevantDocumentIds = new List<Guid> { Guid.NewGuid() }
            };
        }

        #endregion
    }
}
