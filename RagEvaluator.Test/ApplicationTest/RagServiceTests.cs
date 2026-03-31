using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RagEvaluator.Application.Services;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Test.ApplicationTest
{
    public class RagServiceTests
    {
        private readonly RagConfiguration _config;
        private readonly IChatService _chatService;
        private readonly IDocumentService _documentService;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly IQueryService _queryService;
        private readonly RagService _service;

        public RagServiceTests()
        {
            _config = CreateSampleRagConfiguration();
            _chatService = Substitute.For<IChatService>();
            _documentService = Substitute.For<IDocumentService>();
            _documentProcessingService = Substitute.For<IDocumentProcessingService>();
            _queryService = Substitute.For<IQueryService>();
            _service = new RagService(_config, _chatService, _documentService, _documentProcessingService, _queryService);
        }

        #region ProcessDocumentAsync Tests

        [Fact]
        public async Task ProcessDocumentAsync_WithValidInput_ShouldCreateAndProcessDocument()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = new Document { Id = documentId, FileName = "test.pdf" };
            var expectedResponse = new DocumentResponse { Id = documentId, FileName = "test.pdf", Course = "Test Course", Status = "Completed" };
            var stream = new MemoryStream("PDF content"u8.ToArray());

            _documentService.CreateDocumentAsync(stream, "test.pdf", stream.Length, "application/pdf", "en", Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns(document);
            _documentService.GetByIdAsync(documentId, TestContext.Current.CancellationToken)
                .Returns(expectedResponse);

            // Act
            var result = await _service.ProcessDocumentAsync(stream, "test.pdf", "application/pdf", "en", "Test Course", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(documentId, result.Id);
            await _documentService.Received(1).UpdateStatusAsync(documentId, DocumentStatus.Processing, cancellationToken: TestContext.Current.CancellationToken);
            await _documentProcessingService.Received(1).ProcessDocumentContentAsync(documentId, stream, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task ProcessDocumentAsync_WhenProcessingFails_ShouldSetStatusToFailed()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = new Document { Id = documentId, FileName = "test.pdf" };
            var stream = new MemoryStream("PDF content"u8.ToArray());

            _documentService.CreateDocumentAsync(stream, "test.pdf", stream.Length, "application/pdf", "en", Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns(document);
            _documentProcessingService.ProcessDocumentContentAsync(documentId, stream, TestContext.Current.CancellationToken)
                .ThrowsAsync(new Exception("PDF extraction failed"));

            // Act & Assert
            await Assert.ThrowsAsync<Exception>(() =>
                _service.ProcessDocumentAsync(stream, "test.pdf", "application/pdf", "en", "Test Course", TestContext.Current.CancellationToken));

            await _documentService.Received(1).UpdateStatusAsync(documentId, DocumentStatus.Failed, cancellationToken: CancellationToken.None);
        }

        #endregion

        #region AskQuestionAsync Tests

        [Fact]
        public async Task AskQuestionAsync_WithMatchingChunks_ShouldGenerateAnswer()
        {
            // Arrange
            var request = new AskQuestionRequest { Question = "What is cloud?", Language = "en", TopK = 3 };
            var query = CreateSampleQuery();
            var chunkMatches = new List<ChunkSearchMatch>
            {
                new ChunkSearchMatch { Id = Guid.NewGuid(), Text = "Cloud is infrastructure.", Embedding = [0.1f], DocumentId = Guid.NewGuid(), FileName = "doc.pdf" }
            };

            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);
            _queryService.IsReadyAsync(TestContext.Current.CancellationToken).Returns(true);
            _queryService.CreateQueryAsync(
                    request.Question, request.Language, request.TopK,
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    TestContext.Current.CancellationToken)
                .Returns(query);
            _documentProcessingService.SearchChunksAsync(query.QueryEmbedding, query.TopK, TestContext.Current.CancellationToken)
                .Returns(chunkMatches);
            _chatService.GenerateResponseAsync(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken)
                .Returns("Cloud is a computing model.");

            // Act
            var result = await _service.AskQuestionAsync(request, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(query.Id, result.QueryId);
            await _chatService.Received(1).GenerateResponseAsync(Arg.Any<string>(), Arg.Any<string>(), TestContext.Current.CancellationToken);
            await _queryService.Received(1).CompleteQueryAsync(query, "Cloud is a computing model.", Arg.Any<int>(), chunkMatches, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task AskQuestionAsync_WithNoChunks_ShouldReturnFallbackAnswer()
        {
            // Arrange
            var request = new AskQuestionRequest { Question = "What is cloud?", Language = "en", TopK = 3 };
            var query = CreateSampleQuery();

            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);
            _queryService.IsReadyAsync(TestContext.Current.CancellationToken).Returns(true);
            _queryService.CreateQueryAsync(
                    request.Question, request.Language, request.TopK,
                    Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
                    TestContext.Current.CancellationToken)
                .Returns(query);
            _documentProcessingService.SearchChunksAsync(query.QueryEmbedding, query.TopK, TestContext.Current.CancellationToken)
                .Returns(new List<ChunkSearchMatch>());

            // Act
            await _service.AskQuestionAsync(request, TestContext.Current.CancellationToken);

            // Assert
            await _chatService.DidNotReceive().GenerateResponseAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _queryService.Received(1).CompleteQueryAsync(
                query, "No relevant documents found in the knowledge base. Please upload documents first.",
                Arg.Any<int>(), Arg.Is<List<ChunkSearchMatch>>(l => l.Count == 0), TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task AskQuestionAsync_WhenChatServiceUnavailable_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(false);
            _queryService.IsReadyAsync(TestContext.Current.CancellationToken).Returns(true);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AskQuestionAsync(new AskQuestionRequest { Question = "test", Language = "en", TopK = 3 }, TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task AskQuestionAsync_WhenQueryServiceUnavailable_ShouldThrowInvalidOperationException()
        {
            // Arrange
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);
            _queryService.IsReadyAsync(TestContext.Current.CancellationToken).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.AskQuestionAsync(new AskQuestionRequest { Question = "test", Language = "en", TopK = 3 }, TestContext.Current.CancellationToken));
        }

        #endregion

        #region IsInitializedAsync Tests

        [Fact]
        public async Task IsInitializedAsync_WhenBothServicesAvailable_ShouldReturnTrue()
        {
            // Arrange
            _queryService.IsReadyAsync(TestContext.Current.CancellationToken).Returns(true);
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);

            // Act
            var result = await _service.IsInitializedAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task IsInitializedAsync_WhenQueryServiceUnavailable_ShouldReturnFalse()
        {
            // Arrange
            _queryService.IsReadyAsync(TestContext.Current.CancellationToken).Returns(false);
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(true);

            // Act
            var result = await _service.IsInitializedAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task IsInitializedAsync_WhenChatServiceUnavailable_ShouldReturnFalse()
        {
            // Arrange
            _queryService.IsReadyAsync(TestContext.Current.CancellationToken).Returns(true);
            _chatService.IsAvailableAsync(TestContext.Current.CancellationToken).Returns(false);

            // Act
            var result = await _service.IsInitializedAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.False(result);
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
                SimilarityThreshold = 0.5,
                MinChunkSize = 200
            };
        }

        private Query CreateSampleQuery()
        {
            return new Query
            {
                Id = Guid.NewGuid(),
                Question = "What is cloud?",
                Language = "en",
                TopK = 3,
                SystemPrompt = "You are a helpful assistant.",
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                ChatModel = "qwen2.5:14b",
                QueryEmbedding = [0.1f, 0.2f, 0.3f]
            };
        }

        #endregion
    }
}
