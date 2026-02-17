using Microsoft.Extensions.Logging;
using NSubstitute;
using RagEvaluator.Application.Services;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Application.Workers;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class ExperimentServiceTests
    {
        private readonly ILogger<ExperimentService> _logger;
        private readonly IExperimentRepository _experimentRepository;
        private readonly IQueryRepository _queryRepository;
        private readonly IRagService _ragService;
        private readonly ExperimentQueue _experimentQueue;
        private readonly RagConfiguration _config;
        private readonly ExperimentService _service;

        public ExperimentServiceTests()
        {
            _logger = Substitute.For<ILogger<ExperimentService>>();
            _experimentRepository = Substitute.For<IExperimentRepository>();
            _queryRepository = Substitute.For<IQueryRepository>();
            _ragService = Substitute.For<IRagService>();
            _experimentQueue = new ExperimentQueue();
            _config = CreateSampleRagConfiguration();
            _service = new ExperimentService(_logger, _experimentRepository, _queryRepository, _ragService, _experimentQueue, _config);
        }

        #region CreateExperimentAsync Tests

        [Fact]
        public async Task CreateExperimentAsync_WithValidRequest_ShouldCreateExperiment()
        {
            // Arrange
            var request = CreateSampleCreateExperimentRequest();

            // Act
            var response = await _service.CreateExperimentAsync(request, TestContext.Current.CancellationToken);

            // Assert
            await _experimentRepository.Received(1).AddAsync(Arg.Any<Experiment>(), TestContext.Current.CancellationToken);
            Assert.NotNull(response);
            Assert.Equal(request.Name, response.Name);
            Assert.Equal(ExperimentStatus.Running.ToString(), response.Status);
            Assert.Equal(request.RepeatCount, response.RepeatCount);
            Assert.Equal(_config.EmbeddingModel, response.EmbeddingModel);
            Assert.Equal(_config.ChunkingStrategy.ToString(), response.ChunkingStrategy);
            Assert.Equal(_config.PromptTemplate.ToString(), response.PromptTemplate);
        }

        [Fact]
        public async Task CreateExperimentAsync_WithValidRequest_ShouldCalculateTotalQueryCount()
        {
            // Arrange
            var request = CreateSampleCreateExperimentRequest();

            // Act
            var response = await _service.CreateExperimentAsync(request, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(request.Queries.Count * request.RepeatCount, response.Progress.Total);
        }

        #endregion

        #region ProcessExperimentAsync Tests

        [Fact]
        public async Task ProcessExperimentAsync_ExperimentNotFound_ShouldReturnSilently()
        {
            // Arrange
            var experiment = CreateSampleCreateExperimentRequest();
            var queries = experiment.Queries;
            var experimentId = Guid.NewGuid();
            _experimentRepository.GetByIdAsync(experimentId, TestContext.Current.CancellationToken).Returns((Experiment?)null);

            // Act
            await _service.ProcessExperimentAsync(experimentId, queries, TestContext.Current.CancellationToken);

            // Assert
            await _ragService.DidNotReceive().AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ProcessExperimentAsync_WithValidExperiment_ShouldProcessAllQueriesAndComplete()
        {
            // Arrange
            var experiment = CreateSampleExperiment();
            var queries = CreateSampleCreateExperimentRequest().Queries;
            var queryId = Guid.NewGuid();

            _experimentRepository.GetByIdAsync(experiment.Id, TestContext.Current.CancellationToken).Returns(experiment);
            _ragService.AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken)
                .Returns(CreateSampleQueryResponse(queryId));
            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken)
                .Returns(new Query { Id = queryId });

            // Act
            await _service.ProcessExperimentAsync(experiment.Id, queries, TestContext.Current.CancellationToken);

            // Assert — 1 repeat * 2 queries = 2 calls
            await _ragService.Received(2).AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken);
            Assert.Equal(ExperimentStatus.Completed, experiment.Status);
            Assert.NotNull(experiment.CompletedAt);
            Assert.Equal(2, experiment.CompletedQueryCount);
        }

        [Fact]
        public async Task ProcessExperimentAsync_WithRepeatCount_ShouldProcessQueriesMultipleTimes()
        {
            // Arrange
            var experiment = CreateSampleExperiment();
            experiment.RepeatCount = 3;
            var queries = CreateSampleCreateExperimentRequest().Queries;
            var queryId = Guid.NewGuid();

            _experimentRepository.GetByIdAsync(experiment.Id, TestContext.Current.CancellationToken).Returns(experiment);
            _ragService.AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken)
                .Returns(CreateSampleQueryResponse(queryId));
            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken)
                .Returns(new Query { Id = queryId });

            // Act
            await _service.ProcessExperimentAsync(experiment.Id, queries, TestContext.Current.CancellationToken);

            // Assert — 3 repeats * 2 queries = 6 calls
            await _ragService.Received(6).AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken);
            Assert.Equal(6, experiment.CompletedQueryCount);
        }

        [Fact]
        public async Task ProcessExperimentAsync_WhenQueryFails_ShouldContinueProcessingRemainingQueries()
        {
            // Arrange
            var experiment = CreateSampleExperiment();
            var queries = CreateSampleCreateExperimentRequest().Queries;
            var queryId = Guid.NewGuid();

            _experimentRepository.GetByIdAsync(experiment.Id, TestContext.Current.CancellationToken).Returns(experiment);
            _ragService.AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken)
                .Returns(
                    _ => throw new Exception("Embedding service unavailable"),
                    _ => Task.FromResult(CreateSampleQueryResponse(queryId)));
            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken)
                .Returns(new Query { Id = queryId });

            // Act
            await _service.ProcessExperimentAsync(experiment.Id, queries, TestContext.Current.CancellationToken);

            // Assert — first query failed, second succeeded, still completes
            Assert.Equal(ExperimentStatus.Completed, experiment.Status);
            Assert.Equal(1, experiment.CompletedQueryCount);
        }

        [Fact]
        public async Task ProcessExperimentAsync_ShouldLinkQueriesToExperiment()
        {
            // Arrange
            var experiment = CreateSampleExperiment();
            var queries = CreateSampleCreateExperimentRequest().Queries;
            var queryId = Guid.NewGuid();
            var query = new Query { Id = queryId };

            _experimentRepository.GetByIdAsync(experiment.Id, TestContext.Current.CancellationToken).Returns(experiment);
            _ragService.AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken)
                .Returns(CreateSampleQueryResponse(queryId));
            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken)
                .Returns(query);

            // Act
            await _service.ProcessExperimentAsync(experiment.Id, queries, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(experiment.Id, query.ExperimentId);
            await _queryRepository.Received(2).UpdateAsync(query, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ProcessExperimentAsync_WithRelevantDocumentIds_ShouldPopulateGroundTruth()
        {
            // Arrange
            var experiment = CreateSampleExperiment();
            var docId1 = Guid.NewGuid();
            var docId2 = Guid.NewGuid();
            var queries = new List<ExperimentQueryItem>
            {
                new ExperimentQueryItem
                {
                    Question = "What is Serverless Computing?",
                    Language = "en",
                    TopK = 5,
                    RelevantDocumentIds = [docId1, docId2]
                }
            };
            var queryId = Guid.NewGuid();
            var query = new Query { Id = queryId };

            _experimentRepository.GetByIdAsync(experiment.Id, TestContext.Current.CancellationToken).Returns(experiment);
            _ragService.AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken)
                .Returns(CreateSampleQueryResponse(queryId));
            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken)
                .Returns(query);

            // Act
            await _service.ProcessExperimentAsync(experiment.Id, queries, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, query.RelevantDocuments.Count);
            Assert.Contains(query.RelevantDocuments, rd => rd.DocumentId == docId1);
            Assert.Contains(query.RelevantDocuments, rd => rd.DocumentId == docId2);
        }

        [Fact]
        public async Task ProcessExperimentAsync_WithEmptyRelevantDocumentIds_ShouldNotPopulateGroundTruth()
        {
            // Arrange
            var experiment = CreateSampleExperiment();
            var queries = new List<ExperimentQueryItem>
            {
                new ExperimentQueryItem { Question = "What is SQL?", Language = "en", TopK = 3 }
            };
            var queryId = Guid.NewGuid();
            var query = new Query { Id = queryId };

            _experimentRepository.GetByIdAsync(experiment.Id, TestContext.Current.CancellationToken).Returns(experiment);
            _ragService.AskQuestionAsync(Arg.Any<AskQuestionRequest>(), TestContext.Current.CancellationToken)
                .Returns(CreateSampleQueryResponse(queryId));
            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken)
                .Returns(query);

            // Act
            await _service.ProcessExperimentAsync(experiment.Id, queries, TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(query.RelevantDocuments);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetById_WithExistingExperiment_ShouldReturnExperiment()
        {
            // Arrange
            var experiment = CreateSampleExperiment();
            _experimentRepository.GetByIdWithQueriesAsync(experiment.Id, TestContext.Current.CancellationToken).Returns(experiment);

            // Act
            var response = await _service.GetByIdAsync(experiment.Id, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(response);
            Assert.Equal(experiment.Name, response.Name);
            Assert.Equal(experiment.Status.ToString(), response.Status);
        }

        [Fact]
        public async Task GetById_WithNonExistingExperiment_ShouldReturnNull()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            _experimentRepository.GetByIdWithQueriesAsync(experimentId, TestContext.Current.CancellationToken).Returns((Experiment?)null);
            
            // Act
            var response = await _service.GetByIdAsync(experimentId, TestContext.Current.CancellationToken);
            
            // Assert
            Assert.Null(response);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAll_ShouldReturnListOfExperiments()
        {
            // Arrange
            var experiments = new List<Experiment>
            {
                CreateSampleExperiment(),
                CreateSampleExperiment()
            };
            _experimentRepository.GetAllAsync(TestContext.Current.CancellationToken).Returns(experiments);
            
            // Act
            var response = await _service.GetAllAsync(TestContext.Current.CancellationToken);
            
            // Assert
            Assert.NotNull(response);
            Assert.Equal(experiments.Count, response.Count);
        }

        [Fact]
        public async Task GetAll_WhenNoExperiments_ShouldReturnEmptyList()
        {
            // Arrange
            _experimentRepository.GetAllAsync(TestContext.Current.CancellationToken).Returns(new List<Experiment>());
            
            // Act
            var response = await _service.GetAllAsync(TestContext.Current.CancellationToken);
            
            // Assert
            Assert.NotNull(response);
            Assert.Empty(response);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldDelegateToRepository()
        {
            // Arrange
            var experimentId = Guid.NewGuid();

            // Act
            await _service.DeleteAsync(experimentId, TestContext.Current.CancellationToken);

            // Assert
            await _experimentRepository.Received(1).DeleteAsync(experimentId, TestContext.Current.CancellationToken);
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

        private Experiment CreateSampleExperiment()
        {
            return new Experiment
            {
                Id = Guid.NewGuid(),
                Name = "Sample Experiment",
                RepeatCount = 1,
                Status = ExperimentStatus.Running,
                TotalQueryCount = 2,
                CompletedQueryCount = 0,
                EmbeddingModel = _config.EmbeddingModel,
                ChunkingStrategy = _config.ChunkingStrategy.ToString(),
                ChatModel = _config.ChatModel,
                ChunkSize = _config.ChunkSize,
                ChunkOverlap = _config.ChunkOverlap,
                SimilarityThreshold = _config.SimilarityThreshold,
                PromptTemplate = _config.PromptTemplate.ToString()
            };
        }

        private QueryResponse CreateSampleQueryResponse(Guid queryId)
        {
            return new QueryResponse
            {
                QueryId = queryId,
                Question = "What is Serverless Computing?",
                Language = "en",
                SystemPrompt = "You are a helpful assistant.",
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                Answer = "Serverless computing is a cloud execution model.",
                ResponseTimeMs = 150
            };
        }

        private CreateExperimentRequest CreateSampleCreateExperimentRequest()
        {
            return new CreateExperimentRequest
            {
                Name = "Sample Experiment",
                RepeatCount = 2,
                Queries = new List<ExperimentQueryItem>
                {
                    new ExperimentQueryItem
                    {
                        Question = "What is Serverless Computing?",
                        Language = "en",
                        TopK = 5
                    },
                    new ExperimentQueryItem
                    {
                        Question = "Was ist Serverless Computing?",
                        Language = "de",
                        TopK = 5
                    }
                }
            };
        }

        #endregion
    }
}
