using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RagEvaluator.Application.Services;
using RagEvaluator.Application.Workers;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class DocumentServiceTests
    {
        private readonly ILogger<DocumentService> _logger;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly IPdfLoader _pdfLoader;
        private readonly ITextChunker _textChunker;
        private readonly IEmbeddingService _embeddingService;
        private readonly IBackgroundTaskQueue<DocumentProcessingJob> _documentQueue;
        private readonly IBackgroundTaskQueue<DocumentReprocessingJob> _reprocessQueue;
        private readonly RagConfiguration _config;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            _logger = Substitute.For<ILogger<DocumentService>>();
            _documentRepository = Substitute.For<IDocumentRepository>();
            _documentChunkRepository = Substitute.For<IDocumentChunkRepository>();
            _fileStorageService = Substitute.For<IFileStorageService>();
            _pdfLoader = Substitute.For<IPdfLoader>();
            _textChunker = Substitute.For<ITextChunker>();
            _embeddingService = Substitute.For<IEmbeddingService>();
            _documentQueue = Substitute.For<IBackgroundTaskQueue<DocumentProcessingJob>>();
            _reprocessQueue = Substitute.For<IBackgroundTaskQueue<DocumentReprocessingJob>>();
            _config = CreateSampleRagConfiguration();
            _service = new DocumentService(_logger, _documentRepository, _documentChunkRepository, _fileStorageService, _pdfLoader, _textChunker, _embeddingService, _documentQueue, _reprocessQueue, _config);
        }

        private static RagConfiguration CreateSampleRagConfiguration()
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

        #region CreateDocumentAsync Tests

        [Fact]
        public async Task CreateDocumentAsync_WithValidInput_CreatesPendingDocumentAndEnqueuesJob()
        {
            // Arrange
            var stream = new MemoryStream("PDF content"u8.ToArray());
            _documentRepository.GetByNameAsync("test.pdf", Arg.Any<CancellationToken>()).Returns((Document?)null);
            _fileStorageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<Guid>(), "test.pdf", Arg.Any<CancellationToken>())
                .Returns("/storage/some-guid.pdf");

            // Act
            var result = await _service.CreateDocumentAsync(stream, "test.pdf", "application/pdf", "en", "Test Course", TestContext.Current.CancellationToken);

            // Assert — returns the Pending document (FileSize taken from the stream), enqueues a job, no inline processing
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal(stream.Length, result.FileSize);
            Assert.Equal("application/pdf", result.MimeType);
            Assert.Equal("en", result.Language);
            Assert.Equal(DocumentStatus.Pending.ToString(), result.Status);
            await _fileStorageService.Received(1).SaveFileAsync(Arg.Any<Stream>(), result.Id, "test.pdf", TestContext.Current.CancellationToken);
            await _documentRepository.Received(1).AddAsync(Arg.Is<Document>(d => d.Id == result.Id), TestContext.Current.CancellationToken);
            await _documentQueue.Received(1).EnqueueAsync(
                Arg.Is<DocumentProcessingJob>(j => j.DocumentId == result.Id), Arg.Any<CancellationToken>());
            await _documentChunkRepository.DidNotReceive().AddRangeAsync(Arg.Any<List<DocumentChunk>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateDocumentAsync_WithPathInFileName_SanitizesFileName()
        {
            // Arrange
            var stream = new MemoryStream("PDF content"u8.ToArray());
            _fileStorageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<Guid>(), "evil.pdf", Arg.Any<CancellationToken>())
                .Returns("/storage/some-guid.pdf");

            // Act
            var result = await _service.CreateDocumentAsync(stream, "../../../evil.pdf", "application/pdf", "en", "Test Course", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("evil.pdf", result.FileName);
            await _fileStorageService.Received(1).SaveFileAsync(Arg.Any<Stream>(), result.Id, "evil.pdf", TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task CreateDocumentAsync_WhenMetadataPersistFails_DeletesSavedFileAndRethrows()
        {
            // Arrange
            var stream = new MemoryStream("PDF content"u8.ToArray());
            const string filePath = "/storage/some-guid.pdf";
            _fileStorageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<Guid>(), "test.pdf", Arg.Any<CancellationToken>())
                .Returns(filePath);
            _documentRepository.AddAsync(Arg.Any<Document>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("persistence failed"));

            // Act & Assert — the failure propagates, the orphaned file is cleaned up, and no job is enqueued
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.CreateDocumentAsync(stream, "test.pdf", "application/pdf", "en", "Test Course", TestContext.Current.CancellationToken));

            await _fileStorageService.Received(1).DeleteFileAsync(filePath, Arg.Any<CancellationToken>());
            await _documentQueue.DidNotReceive().EnqueueAsync(Arg.Any<DocumentProcessingJob>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region GetByIdAsync Tests

        [Fact]
        public async Task GetByIdAsync_WithExistingDocument_ReturnsDocumentResponse()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _service.GetByIdAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(documentId, result.Id);
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal("Completed", result.Status);
        }

        [Fact]
        public async Task GetByIdAsync_WithNonExistentDocument_ReturnsNull()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act
            var result = await _service.GetByIdAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetByNameAsync Tests

        [Fact]
        public async Task GetByNameAsync_WithExistingDocument_ReturnsDocumentResponse()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            _documentRepository.GetByNameAsync("test.pdf", Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _service.GetByNameAsync("test.pdf", TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(documentId, result.Id);
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal("Completed", result.Status);
        }

        [Fact]
        public async Task GetByNameAsync_WithNonExistentDocument_ReturnsNull()
        {
            // Arrange
            _documentRepository.GetByNameAsync("nonexistent.pdf", Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act
            var result = await _service.GetByNameAsync("nonexistent.pdf", TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(result);
        }

        #endregion

        #region GetAllAsync Tests

        [Fact]
        public async Task GetAllAsync_WithDocuments_ReturnsMappedResponses()
        {
            // Arrange
            var summaries = new List<DocumentSummary>
            {
                CreateSampleSummary(),
                CreateSampleSummary()
            };
            _documentRepository.GetAllSummariesAsync(Arg.Any<CancellationToken>()).Returns(summaries);

            // Act
            var result = await _service.GetAllAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetAllAsync_WhenEmpty_ReturnsEmptyList()
        {
            // Arrange
            _documentRepository.GetAllSummariesAsync(Arg.Any<CancellationToken>())
                .Returns(new List<DocumentSummary>());

            // Act
            var result = await _service.GetAllAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Empty(result);
        }

        #endregion

        #region GetDocumentFileInfoAsync Tests

        [Fact]
        public async Task GetDocumentFileInfoAsync_WithExistingDocument_ReturnsFileInfo()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.FilePath = "/storage/test.pdf";
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _service.GetDocumentFileInfoAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("/storage/test.pdf", result.FilePath);
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal("application/pdf", result.MimeType);
        }

        [Fact]
        public async Task GetDocumentFileInfoAsync_WithNullFilePath_ReturnsNull()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.FilePath = null;
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _service.GetDocumentFileInfoAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDocumentFileInfoAsync_WithNonExistentDocument_ReturnsNull()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act
            var result = await _service.GetDocumentFileInfoAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(result);
        }

        [Fact]
        public async Task GetDocumentFileInfoAsync_WithNullMimeType_DefaultsToApplicationPdf()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.FilePath = "/storage/test.pdf";
            document.MimeType = null;
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _service.GetDocumentFileInfoAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.NotNull(result);
            Assert.Equal("application/pdf", result.MimeType);
        }

        #endregion

        #region DeleteAsync Tests

        [Fact]
        public async Task DeleteAsync_WithExistingDocument_DeletesFileChunksAndDocument()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.FilePath = "/storage/test.pdf";
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.DeleteAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            await _fileStorageService.Received(1).DeleteFileAsync("/storage/test.pdf", CancellationToken.None);
            await _documentChunkRepository.Received(1).DeleteByDocumentIdAsync(documentId, CancellationToken.None);
            await _documentRepository.Received(1).DeleteAsync(documentId, CancellationToken.None);
        }

        [Fact]
        public async Task DeleteAsync_WithNullFilePath_SkipsFileDeletion()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.FilePath = null;
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.DeleteAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            await _fileStorageService.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), CancellationToken.None);
            await _documentChunkRepository.Received(1).DeleteByDocumentIdAsync(documentId, CancellationToken.None);
            await _documentRepository.Received(1).DeleteAsync(documentId, CancellationToken.None);
        }

        [Fact]
        public async Task DeleteAsync_WithNonExistentDocument_StillDeletesChunksAndDocument()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act
            await _service.DeleteAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            await _fileStorageService.DidNotReceive().DeleteFileAsync(Arg.Any<string>(), CancellationToken.None);
            await _documentChunkRepository.Received(1).DeleteByDocumentIdAsync(documentId, CancellationToken.None);
            await _documentRepository.Received(1).DeleteAsync(documentId, CancellationToken.None);
        }

        #endregion

        #region SetStatusAsync Tests

        [Fact]
        public async Task SetStatusAsync_ForwardsToRepository()
        {
            // Arrange
            var documentId = Guid.NewGuid();

            // Act
            await _service.SetStatusAsync(documentId, DocumentStatus.Processing, TestContext.Current.CancellationToken);

            // Assert
            await _documentRepository.Received(1).SetStatusAsync(documentId, DocumentStatus.Processing, Arg.Any<CancellationToken>());
        }

        #endregion

        #region ProcessDocumentAsync Tests

        [Fact]
        public async Task ProcessDocumentAsync_WithValidInput_ExtractsChunksAndUpdatesDocument()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            const string filePath = "/storage/test.pdf";
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());
            var pages = new List<string> { "Page 1 text", "Page 2 text" };
            var chunks = new List<string> { "Chunk 1", "Chunk 2", "Chunk 3" };
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _fileStorageService.OpenReadFileAsync(filePath, Arg.Any<CancellationToken>()).Returns(pdfStream);
            _pdfLoader.LoadPdf(pdfStream).Returns(pages);
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(chunks);
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(embedding);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.ProcessDocumentAsync(documentId, filePath, TestContext.Current.CancellationToken);

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
        public async Task ProcessDocumentAsync_WithCorrectConfig_UsesConfigForChunkEntities()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            const string filePath = "/storage/test.pdf";
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());
            var embedding = new float[] { 0.1f, 0.2f, 0.3f };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _fileStorageService.OpenReadFileAsync(filePath, Arg.Any<CancellationToken>()).Returns(pdfStream);
            _pdfLoader.LoadPdf(pdfStream).Returns(new List<string> { "Page text" });
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk 1" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(embedding);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.ProcessDocumentAsync(documentId, filePath, TestContext.Current.CancellationToken);

            // Assert
            await _documentChunkRepository.Received(1).AddRangeAsync(
                Arg.Is<List<DocumentChunk>>(c =>
                    c[0].ChunkingStrategy == "FixedSize" &&
                    c[0].EmbeddingModel == "nomic-embed-text-v2-moe" &&
                    c[0].DocumentId == documentId),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessDocumentAsync_PassesRawChunkTextForEmbedding()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            const string filePath = "/storage/test.pdf";
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _fileStorageService.OpenReadFileAsync(filePath, Arg.Any<CancellationToken>()).Returns(pdfStream);
            _pdfLoader.LoadPdf(pdfStream).Returns(new List<string> { "Page text" });
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Hello world" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.ProcessDocumentAsync(documentId, filePath, TestContext.Current.CancellationToken);

            // Assert
            await _embeddingService.Received(1).GenerateDocumentEmbeddingAsync(
                "Hello world", Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ProcessDocumentAsync_WhenEmbeddingUnavailable_ThrowsInvalidOperationException()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);

            // Act & Assert — availability is checked before the file is opened
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ProcessDocumentAsync(Guid.NewGuid(), "/storage/test.pdf", TestContext.Current.CancellationToken));
        }

        [Fact]
        public async Task ProcessDocumentAsync_WhenDocumentNotFound_ThrowsArgumentException()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            const string filePath = "/storage/test.pdf";
            var pdfStream = new MemoryStream("PDF content"u8.ToArray());

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _fileStorageService.OpenReadFileAsync(filePath, Arg.Any<CancellationToken>()).Returns(pdfStream);
            _pdfLoader.LoadPdf(pdfStream).Returns(new List<string> { "Page text" });
            _textChunker.CreateDocumentChunksAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new List<string> { "Chunk" });
            _embeddingService.GenerateDocumentEmbeddingAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
                .Returns(new float[] { 0.1f });
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ProcessDocumentAsync(documentId, filePath, TestContext.Current.CancellationToken));
        }

        #endregion

        #region ReprocessAllDocumentsAsync Tests

        [Fact]
        public async Task ReprocessAllDocumentsAsync_QueuesAllReprocessableDocuments()
        {
            // Arrange
            var doc1 = CreateSampleDocument();
            doc1.Content = "Document 1 content";
            var doc2 = CreateSampleDocument();
            doc2.Content = "Document 2 content";
            var documents = new List<Document> { doc1, doc2 };

            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>()).Returns(documents);

            // Act
            var result = await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert — enqueues one job per document and does NOT process inline
            Assert.Equal(2, result.DocumentsQueued);
            await _reprocessQueue.Received(1).EnqueueAsync(
                Arg.Is<DocumentReprocessingJob>(j => j.DocumentId == doc1.Id), Arg.Any<CancellationToken>());
            await _reprocessQueue.Received(1).EnqueueAsync(
                Arg.Is<DocumentReprocessingJob>(j => j.DocumentId == doc2.Id), Arg.Any<CancellationToken>());
            await _documentChunkRepository.DidNotReceive().ReplaceChunksAsync(
                Arg.Any<Guid>(), Arg.Any<IEnumerable<DocumentChunk>>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_MarksAllPendingUpFront()
        {
            // Arrange
            var doc = CreateSampleDocument();
            doc.Content = "Content";
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>())
                .Returns(new List<Document> { doc });

            // Act
            await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert — bulk-marked Pending so a refresh immediately reflects the queued state
            await _documentRepository.Received(1).SetStatusAsync(
                Arg.Is<IEnumerable<Guid>>(ids => ids.Single() == doc.Id), DocumentStatus.Pending, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_WhenNoDocuments_QueuesNothing()
        {
            // Arrange
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetReprocessableAsync(Arg.Any<CancellationToken>())
                .Returns(new List<Document>());

            // Act
            var result = await _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(0, result.DocumentsQueued);
            await _reprocessQueue.DidNotReceive().EnqueueAsync(
                Arg.Any<DocumentReprocessingJob>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReprocessAllDocumentsAsync_WhenEmbeddingUnavailable_ThrowsAndQueuesNothing()
        {
            // Arrange — fail fast before queuing any work
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ReprocessAllDocumentsAsync(TestContext.Current.CancellationToken));

            await _reprocessQueue.DidNotReceive().EnqueueAsync(
                Arg.Any<DocumentReprocessingJob>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region ReprocessDocumentByIdAsync Tests

        [Fact]
        public async Task ReprocessDocumentByIdAsync_WithContent_MarksPendingQueuesAndReturnsPending()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.Content = "Existing content";
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _service.ReprocessDocumentByIdAsync(documentId, TestContext.Current.CancellationToken);

            // Assert — marked Pending up front, enqueued once, processed in the background (not inline)
            await _documentRepository.Received(1).SetStatusAsync(documentId, DocumentStatus.Pending, Arg.Any<CancellationToken>());
            await _reprocessQueue.Received(1).EnqueueAsync(
                Arg.Is<DocumentReprocessingJob>(j => j.DocumentId == documentId), Arg.Any<CancellationToken>());
            await _documentChunkRepository.DidNotReceive().ReplaceChunksAsync(
                Arg.Any<Guid>(), Arg.Any<IEnumerable<DocumentChunk>>(), Arg.Any<CancellationToken>());
            Assert.Equal(DocumentStatus.Pending.ToString(), result.Status);
        }

        [Fact]
        public async Task ReprocessDocumentByIdAsync_WhenNoContent_ThrowsAndQueuesNothing()
        {
            // Arrange — a document without extracted content cannot be reprocessed
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.Content = null;
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ReprocessDocumentByIdAsync(documentId, TestContext.Current.CancellationToken));

            await _reprocessQueue.DidNotReceive().EnqueueAsync(
                Arg.Any<DocumentReprocessingJob>(), Arg.Any<CancellationToken>());
            await _documentRepository.DidNotReceive().SetStatusAsync(
                Arg.Any<Guid>(), Arg.Any<DocumentStatus>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReprocessDocumentByIdAsync_WhenDocumentNotFound_ThrowsAndQueuesNothing()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(true);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.ReprocessDocumentByIdAsync(documentId, TestContext.Current.CancellationToken));

            await _reprocessQueue.DidNotReceive().EnqueueAsync(
                Arg.Any<DocumentReprocessingJob>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ReprocessDocumentByIdAsync_WhenEmbeddingUnavailable_ThrowsAndQueuesNothing()
        {
            // Arrange — fail fast before touching the document or queuing any work
            var documentId = Guid.NewGuid();
            _embeddingService.IsAvailableAsync(Arg.Any<CancellationToken>()).Returns(false);

            // Act & Assert
            await Assert.ThrowsAsync<InvalidOperationException>(() =>
                _service.ReprocessDocumentByIdAsync(documentId, TestContext.Current.CancellationToken));

            await _reprocessQueue.DidNotReceive().EnqueueAsync(
                Arg.Any<DocumentReprocessingJob>(), Arg.Any<CancellationToken>());
            await _documentRepository.DidNotReceive().SetStatusAsync(
                Arg.Any<Guid>(), Arg.Any<DocumentStatus>(), Arg.Any<CancellationToken>());
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

        private DocumentSummary CreateSampleSummary(Guid? id = null)
        {
            return new DocumentSummary
            {
                Id = id ?? Guid.NewGuid(),
                FileName = "test.pdf",
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
