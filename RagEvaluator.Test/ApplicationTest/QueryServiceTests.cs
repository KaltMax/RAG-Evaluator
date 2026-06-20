using NSubstitute;
using RagEvaluator.Application.Services;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
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
        private readonly IChatService _chatService;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly RagConfiguration _config;
        private readonly QueryService _service;

        public QueryServiceTests()
        {
            _embeddingService = Substitute.For<IEmbeddingService>();
            _queryRepository = Substitute.For<IQueryRepository>();
            _metricsService = Substitute.For<IMetricsService>();
            _chatService = Substitute.For<IChatService>();
            _documentChunkRepository = Substitute.For<IDocumentChunkRepository>();
            _config = CreateSampleRagConfiguration();
            _service = new QueryService(_embeddingService, _queryRepository, _metricsService, _chatService, _documentChunkRepository, _config);
        }

        #region AskQuestionAsync Tests

        [Fact]
        public async Task AskQuestionAsync_WithMatchingChunks_ShouldGenerateAnswerAndPersist()
        {
            // Arrange
            var request = new AskQuestionRequest { Question = "What is Cloud Computing?", Language = "en", TopK = 3 };
            var chunkMatches = new List<ChunkSearchMatch> { CreateSampleChunkSearchMatch() };

            _chatService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _embeddingService.GenerateQueryEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f, 0.2f, 0.3f });
            _documentChunkRepository.SearchAsync(Arg.Any<float[]>(), request.TopK, Arg.Any<CancellationToken>())
                .Returns(chunkMatches);
            _metricsService.CosineSimilarity(Arg.Any<float[]>(), Arg.Any<float[]>()).Returns(0.9f);
            _chatService.GenerateResponseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns("Cloud computing is a model.");

            // Act
            var result = await _service.AskQuestionAsync(request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("Cloud computing is a model.", result.Answer);
            Assert.Equal(request.Question, result.Question);
            await _chatService.Received(1).GenerateResponseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _queryRepository.Received(1).AddAsync(Arg.Any<Query>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AskQuestionAsync_WithNoChunks_ShouldReturnFallbackAndSkipChat()
        {
            // Arrange
            var request = new AskQuestionRequest { Question = "What is Cloud Computing?", Language = "en", TopK = 3 };

            _chatService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _embeddingService.GenerateQueryEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });
            _documentChunkRepository.SearchAsync(Arg.Any<float[]>(), request.TopK, Arg.Any<CancellationToken>())
                .Returns(new List<ChunkSearchMatch>());

            // Act
            var result = await _service.AskQuestionAsync(request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Contains("No relevant documents", result.Answer);
            await _chatService.DidNotReceive().GenerateResponseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _queryRepository.Received(1).AddAsync(Arg.Any<Query>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AskQuestionAsync_WhenChatServiceUnavailable_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _chatService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AskQuestionAsync(new AskQuestionRequest { Question = "test", Language = "en", TopK = 3 }, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AskQuestionAsync_WhenEmbeddingServiceUnavailable_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _chatService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AskQuestionAsync(new AskQuestionRequest { Question = "test", Language = "en", TopK = 3 }, TestContext.Current.CancellationToken));
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
                new QueryResultAnnotation { ResultId = resultId1, RelevanceGrade = RelevanceGrade.Relevant },
                new QueryResultAnnotation { ResultId = resultId2, RelevanceGrade = RelevanceGrade.NotRelevant }
            };

            // Act
            await _service.AnnotateResultsAsync(
                queryId, annotations, ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId1 }, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(RelevanceGrade.Relevant, query.Results.First(r => r.Id == resultId1).RelevanceGrade);
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
                new QueryResultAnnotation { ResultId = resultId1, RelevanceGrade = RelevanceGrade.Relevant }
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

        [Fact]
        public async Task AnnotateResultsAsync_ShouldPropagateGradesToUnannotatedSiblings()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            var queryId = Guid.NewGuid();
            var resultId = Guid.NewGuid();
            var docId = Guid.NewGuid();

            var query = CreateSampleQuery();
            query.ExperimentId = experimentId;
            query.Results = new List<QueryResult>
            {
                new QueryResult { Id = resultId, QueryId = queryId, DocumentId = docId, Rank = 1, ChunkText = "Chunk A" }
            };

            var siblingResultId = Guid.NewGuid();
            var sibling = CreateSampleQuery();
            sibling.ExperimentId = experimentId;
            sibling.Results = new List<QueryResult>
            {
                new QueryResult { Id = siblingResultId, QueryId = sibling.Id, DocumentId = docId, Rank = 1, ChunkText = "Chunk A" }
            };

            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken).Returns(query);
            _queryRepository.GetUnannotatedSiblingsAsync(
                query.Id, experimentId, query.Question, query.Language, query.TopK, Arg.Any<CancellationToken>())
                .Returns(new List<Query> { sibling });
            _metricsService.CalculateQueryMetrics(Arg.Any<IReadOnlyList<QueryResult>>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<Guid>>())
                .Returns(new QueryMetrics { MRR = 1.0, PrecisionAtK = 1.0, RecallAtK = 1.0, NDCGAtK = 1.0 });

            var annotations = new List<QueryResultAnnotation>
            {
                new QueryResultAnnotation { ResultId = resultId, RelevanceGrade = RelevanceGrade.Relevant }
            };

            // Act
            await _service.AnnotateResultsAsync(
                queryId, annotations, ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId }, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(RelevanceGrade.Relevant, sibling.Results.First().RelevanceGrade);
            Assert.True(sibling.Results.First().IsRelevant);
            await _queryRepository.Received(1).UpdateAsync(sibling, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AnnotateResultsAsync_ShouldNotPropagateForNonExperimentQueries()
        {
            // Arrange
            var (query, queryId, resultId1, _, docId1, _) = ArrangeAnnotation();
            query.ExperimentId = null;

            var annotations = new List<QueryResultAnnotation>
            {
                new QueryResultAnnotation { ResultId = resultId1, RelevanceGrade = RelevanceGrade.Relevant }
            };

            // Act
            await _service.AnnotateResultsAsync(
                queryId, annotations, ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId1 }, TestContext.Current.CancellationToken);

            // Assert
            await _queryRepository.DidNotReceive().GetUnannotatedSiblingsAsync(
                Arg.Any<Guid>(), Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<int>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task AnnotateResultsAsync_ShouldNotOverwriteExistingSiblingGrades()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            var queryId = Guid.NewGuid();
            var resultId = Guid.NewGuid();
            var docId = Guid.NewGuid();

            var query = CreateSampleQuery();
            query.ExperimentId = experimentId;
            query.Results = new List<QueryResult>
            {
                new QueryResult { Id = resultId, QueryId = queryId, DocumentId = docId, Rank = 1, ChunkText = "Chunk A" }
            };

            var sibling = CreateSampleQuery();
            sibling.ExperimentId = experimentId;
            sibling.Results = new List<QueryResult>
            {
                new QueryResult
                {
                    Id = Guid.NewGuid(), QueryId = sibling.Id, DocumentId = docId, Rank = 1,
                    ChunkText = "Chunk A", RelevanceGrade = RelevanceGrade.NotRelevant, IsRelevant = false
                }
            };

            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken).Returns(query);
            _queryRepository.GetUnannotatedSiblingsAsync(
                query.Id, experimentId, query.Question, query.Language, query.TopK, Arg.Any<CancellationToken>())
                .Returns(new List<Query> { sibling });
            _metricsService.CalculateQueryMetrics(Arg.Any<IReadOnlyList<QueryResult>>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<Guid>>())
                .Returns(new QueryMetrics { MRR = 1.0, PrecisionAtK = 1.0, RecallAtK = 1.0, NDCGAtK = 1.0 });

            var annotations = new List<QueryResultAnnotation>
            {
                new QueryResultAnnotation { ResultId = resultId, RelevanceGrade = RelevanceGrade.Relevant }
            };

            // Act
            await _service.AnnotateResultsAsync(
                queryId, annotations, ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId }, TestContext.Current.CancellationToken);

            // Assert — sibling's existing grade should NOT be overwritten
            Assert.Equal(RelevanceGrade.NotRelevant, sibling.Results.First().RelevanceGrade);
            Assert.False(sibling.Results.First().IsRelevant);
        }

        [Fact]
        public async Task AnnotateResultsAsync_ShouldLeaveNonMatchingChunksUnannotated()
        {
            // Arrange
            var experimentId = Guid.NewGuid();
            var queryId = Guid.NewGuid();
            var resultId = Guid.NewGuid();
            var docId = Guid.NewGuid();

            var query = CreateSampleQuery();
            query.ExperimentId = experimentId;
            query.Results = new List<QueryResult>
            {
                new QueryResult { Id = resultId, QueryId = queryId, DocumentId = docId, Rank = 1, ChunkText = "Chunk A" }
            };

            var sibling = CreateSampleQuery();
            sibling.ExperimentId = experimentId;
            sibling.Results = new List<QueryResult>
            {
                new QueryResult { Id = Guid.NewGuid(), QueryId = sibling.Id, DocumentId = docId, Rank = 1, ChunkText = "Chunk B" }
            };

            _queryRepository.GetByIdWithResultsAsync(queryId, TestContext.Current.CancellationToken).Returns(query);
            _queryRepository.GetUnannotatedSiblingsAsync(
                query.Id, experimentId, query.Question, query.Language, query.TopK, Arg.Any<CancellationToken>())
                .Returns(new List<Query> { sibling });
            _metricsService.CalculateQueryMetrics(Arg.Any<IReadOnlyList<QueryResult>>(), Arg.Any<int>(), Arg.Any<IReadOnlyList<Guid>>())
                .Returns(new QueryMetrics { MRR = 1.0, PrecisionAtK = 1.0, RecallAtK = 1.0, NDCGAtK = 1.0 });

            var annotations = new List<QueryResultAnnotation>
            {
                new QueryResultAnnotation { ResultId = resultId, RelevanceGrade = RelevanceGrade.Relevant }
            };

            // Act
            await _service.AnnotateResultsAsync(
                queryId, annotations, ResponseQuality.CorrectAndComplete, false,
                new List<Guid> { docId }, TestContext.Current.CancellationToken);

            // Assert — non-matching chunk stays unannotated
            Assert.Null(sibling.Results.First().RelevanceGrade);
            Assert.Null(sibling.Results.First().IsRelevant);
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
                SimilarityThreshold = 0.5,
                MinChunkSize = 200
            };
        }

        #endregion
    }
}
