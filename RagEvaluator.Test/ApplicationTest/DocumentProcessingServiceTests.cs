using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RagEvaluator.Application.Services;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Test.ApplicationTest
{
    public class DocumentProcessingServiceTests
    {
        private readonly ILogger<DocumentProcessingService> _logger;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IPdfLoader _pdfLoader;
        private readonly ITextChunker _textChunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly RagConfiguration _config;
        private readonly DocumentProcessingService _service;

        public DocumentProcessingServiceTests()
        {
            _logger = Substitute.For<ILogger<DocumentProcessingService>>();
            _documentRepository = Substitute.For<IDocumentRepository>();
            _documentChunkRepository = Substitute.For<IDocumentChunkRepository>();
            _pdfLoader = Substitute.For<IPdfLoader>();
            _textChunker = Substitute.For<ITextChunker>();
            _embeddingService = Substitute.For<IEmbeddingService>();
            _config = CreateSampleRagConfiguration();
            _service = new DocumentProcessingService(
                _logger,
                _documentRepository,
                _documentChunkRepository,
                _pdfLoader,
                _textChunker,
                _embeddingService,
                _config);
        }

        #region ProcessDocumentContentAsync Tests

        [Fact]
        public async Task ProcessDocumentContentAsync_WithValidInput_ExtractsChunksAndUpdatesDocument()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());
            var pages = new List<string> { "Page 1 text", "Page 2 text" };
            var chunks = new List<string> { "Chunk 1", "Chunk 2", "Chunk 3" };
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _pdfLoader.LoadPdf(pdfStream).Returns(pages);
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(chunks);
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(embedding);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.ProcessDocumentContentAsync(documentId, pdfStream, TestContext.Current.CancellationToken);

            // Assert
            await _documentChunkRepository.Received(1).AddRangeAsync(
                Arg.Is<List<DocumentChunk>>(c => c.Count == 3), Arg.Any<CancellationToken>());
            Assert.Equal(DocumentStatus.Completed, document.Status);
            Assert.Equal(2, document.PageCount);
            Assert.Equal(3, document.ChunkCount);
            Assert.Equal("Page 1 text\n\nPage 2 text", document.Content);
            Assert.NotNull(document.ProcessedAt);
            await _documentRepository.Received(1).UpdateAsync(document, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessDocumentContentAsync_WithCorrectConfig_UsesConfigForChunkEntities()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _pdfLoader.LoadPdf(pdfStream).Returns(new List<string> { "Page text" });
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk 1" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(embedding);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.ProcessDocumentContentAsync(documentId, pdfStream, TestContext.Current.CancellationToken);

            // Assert
            await _documentChunkRepository.Received(1).AddRangeAsync(
                Arg.Is<List<DocumentChunk>>(c =>
                    c[0].ChunkingStrategy == "FixedSize" &&
                    c[0].EmbeddingModel == "nomic-embed-text-v2-moe" &&
                    c[0].DocumentId == documentId),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessDocumentContentAsync_PassesRawChunkTextForEmbedding()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _pdfLoader.LoadPdf(pdfStream).Returns(new List<string> { "Page text" });
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Hello world" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.ProcessDocumentContentAsync(documentId, pdfStream, TestContext.Current.CancellationToken);

            // Assert
            await _embeddingService.Received(1).GenerateDocumentEmbeddingAsync(
                "Hello world", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessDocumentContentAsync_WhenEmbeddingUnavailable_ThrowsInvalidOperationException()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ProcessDocumentContentAsync(Guid.NewGuid(), new MemoryStream(), TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ProcessDocumentContentAsync_WhenDocumentNotFound_ThrowsArgumentException()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _pdfLoader.LoadPdf(pdfStream).Returns(new List<string> { "Page text" });
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ProcessDocumentContentAsync(documentId, pdfStream, TestContext.Current.CancellationToken));
        }

        #endregion

        #region ReprocessAllDocumentsAsync Tests

        [Fact]
        public async Task ReprocessAllDocumentsAsync_WithDocuments_SwapsChunksAndReprocesses()
        {
            // Arrange
            var doc1 = CreateSampleDocument();
            doc1.Content = "Document 1 content";
            var doc2 = CreateSampleDocument();
            doc2.Content = "Document 2 content";
            var documents = new List<Document> { doc1, doc2 };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>()).Returns(documents);
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk A", "Chunk B" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f, 0.2f });

            // Act
            var result = await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result.DocumentsProcessed);
            Assert.Equal(0, result.DocumentsFailed);
            Assert.Equal(4, result.TotalChunksCreated);
            Assert.Equal("FixedSize", result.ChunkingStrategy);
            Assert.Equal("nomic-embed-text-v2-moe", result.EmbeddingModel);
            await _documentChunkRepository.Received(1).ReplaceChunksAsync(doc1.Id, Arg.Any<IEnumerable<DocumentChunk>>(), Arg.Any<CancellationToken>());
            await _documentChunkRepository.Received(1).ReplaceChunksAsync(doc2.Id, Arg.Any<IEnumerable<DocumentChunk>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_MarksAllProcessingUpFrontThenCompleted()
        {
            // Arrange
            var doc = CreateSampleDocument();
            doc.Content = "Content";
            var documents = new List<Document> { doc };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>()).Returns(documents);
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });

            var statusHistory = new List<DocumentStatus>();
            _documentRepository.When(r => r.UpdateAsync(doc, Arg.Any<CancellationToken>()))
                .Do(_ => statusHistory.Add(doc.Status));

            // Act
            await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert — all marked Processing up front via a single bulk update, then each set Completed
            await _documentRepository.Received(1).SetStatusAsync(
                Arg.Any<IEnumerable<Guid>>(), DocumentStatus.Processing, Arg.Any<CancellationToken>());
            Assert.Single(statusHistory);
            Assert.Equal(DocumentStatus.Completed, statusHistory[0]);
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_WhenDocumentFails_MarksFailedAndContinues()
        {
            // Arrange
            var failingDoc = CreateSampleDocument();
            failingDoc.Content = "fail";
            var okDoc = CreateSampleDocument();
            okDoc.Content = "ok";
            var documents = new List<Document> { failingDoc, okDoc };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>()).Returns(documents);
            _textChunker.CreateDocumentChunksAsync("fail", Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("boom"));
            _textChunker.CreateDocumentChunksAsync("ok", Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });

            // Act
            var result = await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert — the failure is isolated: it is marked Failed, the rest still completes
            Assert.Equal(2, result.DocumentsProcessed);
            Assert.Equal(1, result.DocumentsFailed);
            Assert.Equal(1, result.TotalChunksCreated);
            Assert.Equal(DocumentStatus.Failed, failingDoc.Status);
            Assert.Equal(DocumentStatus.Completed, okDoc.Status);
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_ReprocessesPreviouslyFailedDocuments()
        {
            // Arrange — a document left in Failed state (but with content) is still reprocessed, not skipped
            var failedDoc = CreateSampleDocument();
            failedDoc.Content = "recoverable content";
            failedDoc.Status = DocumentStatus.Failed;

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>())
                .Returns(new List<Document> { failedDoc });
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });

            // Act
            var result = await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert — the formerly Failed document is recovered to Completed
            Assert.Equal(1, result.DocumentsProcessed);
            Assert.Equal(0, result.DocumentsFailed);
            Assert.Equal(DocumentStatus.Completed, failedDoc.Status);
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_WhenNoDocuments_ReturnsZeroCounts()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>())
                .Returns(new List<Document>());

            // Act
            var result = await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(0, result.DocumentsProcessed);
            Assert.Equal(0, result.DocumentsFailed);
            Assert.Equal(0, result.TotalChunksCreated);
            await _documentChunkRepository.DidNotReceive().ReplaceChunksAsync(Arg.Any<Guid>(), Arg.Any<IEnumerable<DocumentChunk>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_WhenEmbeddingUnavailable_ThrowsInvalidOperationException()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken));
        }

        #endregion

        #region GetChunksByDocumentIdAsync Tests

        [Fact]
        public async Task GetChunksByDocumentIdAsync_WithChunks_ReturnsMappedResponses()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var chunks = new List<DocumentChunk>
            {
                CreateSampleDocumentChunk(documentId: documentId),
                CreateSampleDocumentChunk(documentId: documentId)
            };
            _documentChunkRepository.GetByDocumentIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(chunks);

            // Act
            var result = await _service.GetChunksByDocumentIdAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result.Count);
            Assert.Equal(documentId, result[0].DocumentId);
        }

        [Fact]
        public async Task GetChunksByDocumentIdAsync_WhenNoChunks_ReturnsEmptyList()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentChunkRepository.GetByDocumentIdAsync(documentId, Arg.Any<CancellationToken>())
                .Returns(new List<DocumentChunk>());

            // Act
            var result = await _service.GetChunksByDocumentIdAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(result);
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

        private Document CreateSampleDocument(Guid? id = null)
        {
            return new Document
            {
                Id = id ?? Guid.NewGuid(),
                FileName = "test.pdf",
                FilePath = "/storage/test.pdf",
                FileSize = 1024,
                MimeType = "application/pdf",
                Language = "en",
                PageCount = 10,
                ChunkCount = 5,
                UploadedAt = DateTime.UtcNow,
                ProcessedAt = DateTime.UtcNow,
                Status = DocumentStatus.Completed
            };
        }

        private DocumentChunk CreateSampleDocumentChunk(Guid? id = null, Guid? documentId = null)
        {
            return new DocumentChunk
            {
                Id = id ?? Guid.NewGuid(),
                Text = "Sample chunk text.",
                Embedding = new float[] { 0.1f, 0.2f, 0.3f },
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe",
                DocumentId = documentId ?? Guid.NewGuid()
            };
        }

        #endregion
    }
}
