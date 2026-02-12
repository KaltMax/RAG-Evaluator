using NSubstitute;
using RagEvaluator.Application.Services;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Test.ApplicationTest
{
    public class QueryServiceTests
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IQueryRepository _queryRepository;
        private readonly IMetricsService _metricsService;
        private readonly QueryService _service;

        public QueryServiceTests()
        {
            _embeddingService = Substitute.For<IEmbeddingService>();
            _queryRepository = Substitute.For<IQueryRepository>();
            _metricsService = Substitute.For<IMetricsService>();
            _service = new QueryService(_embeddingService, _queryRepository, _metricsService);
        }

        #region IsReadyAsync Tests

        [Fact]
        public async Task IsReadyAsync_ReturnsTrue_WhenAllDependenciesAreReady()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);

            // Act
            var result = await _service.IsReadyAsync(TestContext.Current.CancellationToken);
            
            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsReadyAsync_ReturnsFalse_WhenEmbeddingServiceIsNotReady()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);
            
            // Act
            var result = await _service.IsReadyAsync(TestContext.Current.CancellationToken);
            
            // Assert
            Assert.False(result);
        }

        #endregion

        #region CreateQueryAsync Tests

        [Fact]
        public async Task CreateQueryAsync_ShouldReturnQueryWithEmbedding()
        {
            // Arrange
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };
            _embeddingService.GenerateEmbeddingAsync("search_query: What is Cloud Computing?", TestContext.Current.CancellationToken)
                .Returns(embedding);

            // Act
            var result = await _service.CreateQueryAsync(
                "What is Cloud Computing?", "en", 5,
                "You are a helpful assistant.", "FixedSize",
                "nomic-embed-text-v2-moe", "qwen2.5:14b",
                TestContext.Current.CancellationToken);

            // Assert
            Assert.NotEqual(Guid.Empty, result.Id);
            Assert.Equal("What is Cloud Computing?", result.Question);
            Assert.Equal("en", result.Language);
            Assert.Equal(5, result.TopK);
            Assert.Equal(embedding, result.QueryEmbedding);
        }

        [Fact]
        public async Task CreateQueryAsync_ShouldPrefixQuestionForEmbedding()
        {
            // Arrange
            _embeddingService.GenerateEmbeddingAsync(Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns(new float[] { 0.1f });

            // Act
            await _service.CreateQueryAsync(
                "Test question", "en", 3,
                "prompt", "FixedSize", "model", "chat",
                TestContext.Current.CancellationToken);

            // Assert
            await _embeddingService.Received(1).GenerateEmbeddingAsync(
                "search_query: Test question", TestContext.Current.CancellationToken);
        }

        #endregion

        #region CompleteQueryAsync Tests

        [Fact]
        public async Task CompleteQueryAsync_ShouldCompleteQueryAndSaveResults()
        {
            // Arrange
            var query = CreateSampleQuery();
            var answer = "Cloud computing is the delivery of computing services over the internet.";
            var responseTimeMs = 1500;
            var chunkMatches = new List<ChunkSearchMatch> { CreateSampleChunkSearchMatch() };
            _metricsService.CosineSimilarity(Arg.Any<float[]>(), Arg.Any<float[]>()).Returns(0.95f);
            
            // Act
            await _service.CompleteQueryAsync(query, answer, responseTimeMs, chunkMatches, TestContext.Current.CancellationToken);
            
            // Assert
            Assert.Equal(answer, query.Answer);
            Assert.Equal(responseTimeMs, query.ResponseTimeMs);
            Assert.Single(query.Results);
            Assert.Equal(0.95f, query.Results.First().SimilarityScore);
            await _queryRepository.Received(1).AddAsync(query, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task CompleteQueryAsync_ShouldHandleEmptyChunkMatches()
        {
            // Arrange
            var query = CreateSampleQuery();
            var answer = "Cloud computing is the delivery of computing services over the internet.";
            var responseTimeMs = 1500;
            var chunkMatches = new List<ChunkSearchMatch>();
            
            // Act
            await _service.CompleteQueryAsync(query, answer, responseTimeMs, chunkMatches, TestContext.Current.CancellationToken);
            
            // Assert
            Assert.Equal(answer, query.Answer);
            Assert.Equal(responseTimeMs, query.ResponseTimeMs);
            Assert.Empty(query.Results);
            await _queryRepository.Received(1).AddAsync(query, TestContext.Current.CancellationToken);
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_ShouldReturnQueryResponse()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            var expectedResponse = CreateSampleQuery();
            _queryRepository.GetByIdWithResultsAsync(queryId, Arg.Any<CancellationToken>()).Returns(expectedResponse);
            
            // Act
            var result = await _service.GetByIdAsync(queryId, TestContext.Current.CancellationToken);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedResponse.Id, result!.QueryId);
            Assert.Equal(expectedResponse.Question, result.Question);
            Assert.Equal(expectedResponse.Answer, result.Answer);
        }

        [Fact]
        public async Task GetByIdAsync_ShouldReturnNull_WhenQueryNotFound()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            _queryRepository.GetByIdWithResultsAsync(queryId, Arg.Any<CancellationToken>()).Returns((Query?)null);
            
            // Act
            var result = await _service.GetByIdAsync(queryId, TestContext.Current.CancellationToken);
            
            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_ShouldReturnListOfQuerySummaries()
        {
            // Arrange
            var expectedSummaries = new List<Query>
            {
                CreateSampleQuery(),
                CreateSampleQuery()
            };
            _queryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(expectedSummaries);
            
            // Act
            var result = await _service.GetAllAsync(TestContext.Current.CancellationToken);
            
            // Assert
            Assert.NotNull(result);
            Assert.Equal(expectedSummaries.Count, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoQueries()
        {
            // Arrange
            _queryRepository.GetAllAsync(Arg.Any<CancellationToken>()).Returns(new List<Query>());
            
            // Act
            var result = await _service.GetAllAsync(TestContext.Current.CancellationToken);
            
            // Assert
            Assert.NotNull(result);
            Assert.Empty(result);
        }

        #endregion

        #region AnnotateResultsAsync Tests

        [Fact]
        public async Task AnnotateResultsAsync_ShouldApplyRelevanceGrades()
        {
            // Arrange
            var (query, queryId, resultId1, resultId2, docId1, _) = ArrangeAnnotation();

            var annotations = new List<QueryResultAnnotation>
            {
                new QueryResultAnnotation { ResultId = resultId1, RelevanceGrade = RelevanceGrade.HighlyRelevant },
                new QueryResultAnnotation { ResultId = resultId2, RelevanceGrade = RelevanceGrade.NotRelevant }
            };

            // Act
            await _service.AnnotateResultsAsync(
                queryId, annotations, ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId1 }, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(RelevanceGrade.HighlyRelevant, query.Results.First(r => r.Id == resultId1).RelevanceGrade);
            Assert.True(query.Results.First(r => r.Id == resultId1).IsRelevant);
            Assert.Equal(RelevanceGrade.NotRelevant, query.Results.First(r => r.Id == resultId2).RelevanceGrade);
            Assert.False(query.Results.First(r => r.Id == resultId2).IsRelevant);
        }

        [Fact]
        public async Task AnnotateResultsAsync_ShouldSetResponseQualityAndLanguageSwitching()
        {
            // Arrange
            var (query, queryId, _, _, docId1, _) = ArrangeAnnotation();

            // Act
            await _service.AnnotateResultsAsync(
                queryId, new List<QueryResultAnnotation>(),
                ResponseQuality.VagueOrIncomplete, true,
                new List<Guid> { docId1 }, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(ResponseQuality.VagueOrIncomplete, query.ResponseQuality);
            Assert.True(query.HasLanguageSwitching);
        }

        [Fact]
        public async Task AnnotateResultsAsync_ShouldSetGroundTruthDocuments()
        {
            // Arrange
            var (query, queryId, _, _, docId1, docId2) = ArrangeAnnotation();

            // Act
            await _service.AnnotateResultsAsync(
                queryId, new List<QueryResultAnnotation>(),
                ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId1, docId2 }, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, query.RelevantDocuments.Count);
            Assert.Contains(query.RelevantDocuments, rd => rd.DocumentId == docId1);
            Assert.Contains(query.RelevantDocuments, rd => rd.DocumentId == docId2);
        }

        [Fact]
        public async Task AnnotateResultsAsync_ShouldCalculateAndSaveMetrics()
        {
            // Arrange
            var (query, queryId, resultId1, _, docId1, _) = ArrangeAnnotation();

            var annotations = new List<QueryResultAnnotation>
            {
                new QueryResultAnnotation { ResultId = resultId1, RelevanceGrade = RelevanceGrade.HighlyRelevant }
            };

            // Act
            await _service.AnnotateResultsAsync(
                queryId, annotations, ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId1 }, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(1.0, query.MRR);
            Assert.Equal(0.5, query.PrecisionAtK);
            Assert.Equal(1.0, query.RecallAtK);
            Assert.Equal(0.8, query.NDCGAtK);
            await _queryRepository.Received(1).UpdateAsync(query, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task AnnotateResultsAsync_WhenQueryNotFound_ShouldThrowArgumentException()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken).Returns((Query?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.AnnotateResultsAsync(
                    queryId, new List<QueryResultAnnotation>(),
                    ResponseQuality.CorrectAndComplete, false,
                    new List<Guid>(), TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AnnotateResultsAsync_WithDuplicateRelevantDocumentIds_ShouldDeduplicateThem()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            var docId = Guid.NewGuid();
            var query = CreateSampleQuery();
            query.Results = new List<QueryResult>();

            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken).Returns(query);

            // Act
            await _service.AnnotateResultsAsync(
                queryId, new List<QueryResultAnnotation>(),
                ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId, docId, docId }, TestContext.Current.CancellationToken);

            // Assert — duplicates removed via .Distinct()
            Assert.Single(query.RelevantDocuments);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_ShouldDelegateToRepository()
        {
            // Arrange
            var queryId = Guid.NewGuid();
            
            // Act
            await _service.DeleteAsync(queryId, TestContext.Current.CancellationToken);
            
            // Assert
            await _queryRepository.Received(1).DeleteAsync(queryId, TestContext.Current.CancellationToken);
        }

        #endregion

        #region Helper Methods

        private (Query query, Guid queryId, Guid resultId1, Guid resultId2, Guid docId1, Guid docId2) ArrangeAnnotation()
        {
            var queryId = Guid.NewGuid();
            var resultId1 = Guid.NewGuid();
            var resultId2 = Guid.NewGuid();
            var docId1 = Guid.NewGuid();
            var docId2 = Guid.NewGuid();

            var query = CreateSampleQuery();
            query.Results = new List<QueryResult>
            {
                new QueryResult { Id = resultId1, QueryId = queryId, DocumentId = docId1, Rank = 1 },
                new QueryResult { Id = resultId2, QueryId = queryId, DocumentId = docId2, Rank = 2 }
            };

            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken).Returns(query);
            _metricsService.CalculateQueryMetrics(Arg.Any<IReadOnlyList<QueryResult>>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<Guid>>())
                .Returns(new QueryMetrics { MRR = 1.0, PrecisionAtK = 0.5, RecallAtK = 1.0, NDCGAtK = 0.8 });

            return (query, queryId, resultId1, resultId2, docId1, docId2);
        }

        private Query CreateSampleQuery() 
        {
            return new Query
            {
                Id = Guid.NewGuid(),
                Question = "What is Cloud Computing?",
                Language = "en",
                TopK = 5,
                SystemPrompt = "You are a helpful assistant.",
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                CreatedAt = DateTime.UtcNow
            };
        }

        private ChunkSearchMatch CreateSampleChunkSearchMatch()
        {
            return new ChunkSearchMatch
            {
                Id = Guid.NewGuid(),
                Text = "Cloud computing is the delivery of computing services over the internet.",
                DocumentId = Guid.NewGuid(),
                FileName = "cloud_computing.pdf",
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                Embedding = new float[] { 0.1f, 0.2f, 0.3f }
            };
        }

        #endregion
    }
}
