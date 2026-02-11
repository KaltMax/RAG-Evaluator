using NSubstitute;
using RagEvaluator.Application.Services;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class DocumentServiceTests
    {
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            _documentRepository = Substitute.For<IDocumentRepository>();
            _documentChunkRepository = Substitute.For<IDocumentChunkRepository>();
            _fileStorageService = Substitute.For<IFileStorageService>();
            _service = new DocumentService(_documentRepository, _documentChunkRepository, _fileStorageService);
        }

        #region CreateDocumentAsync Tests

        [Fact]
        public async Task CreateDocumentAsync_WithValidInput_ReturnsDocumentWithCorrectFields()
        {
            // Arrange
            var stream = new MemoryStream("PDF content"u8.ToArray());
            var expectedFilePath = "/storage/some-guid.pdf";
            _fileStorageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<Guid>(), "test.pdf", Arg.Any<CancellationToken>())
                .Returns(expectedFilePath);

            // Act
            var result = await _service.CreateDocumentAsync(stream, "test.pdf", 1024, "application/pdf", "en", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal(expectedFilePath, result.FilePath);
            Assert.Equal(1024, result.FileSize);
            Assert.Equal("application/pdf", result.MimeType);
            Assert.Equal("en", result.Language);
            Assert.Equal(DocumentStatus.Pending, result.Status);
            await _fileStorageService.Received(1).SaveFileAsync(Arg.Any<Stream>(), result.Id, "test.pdf", TestContext.Current.CancellationToken);
            await _documentRepository.Received(1).AddAsync(result, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task CreateDocumentAsync_WithPathInFileName_SanitizesFileName()
        {
            // Arrange
            var stream = new MemoryStream("PDF content"u8.ToArray());
            _fileStorageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<Guid>(), "evil.pdf", Arg.Any<CancellationToken>())
                .Returns("/storage/some-guid.pdf");

            // Act
            var result = await _service.CreateDocumentAsync(stream, "../../../evil.pdf", 1024, "application/pdf", "en", TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal("evil.pdf", result.FileName);
            await _fileStorageService.Received(1).SaveFileAsync(Arg.Any<Stream>(), result.Id, "evil.pdf", TestContext.Current.CancellationToken);
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

        #region UpdateStatusAsync Tests

        [Fact]
        public async Task UpdateStatusAsync_WithValidId_UpdatesStatus()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.Status = DocumentStatus.Pending;
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.UpdateStatusAsync(documentId, DocumentStatus.Processing, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(DocumentStatus.Processing, document.Status);
            await _documentRepository.Received(1).UpdateAsync(document, TestContext.Current.CancellationToken);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithCompleted_SetsProcessedAt()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.ProcessedAt = null;
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.UpdateStatusAsync(documentId, DocumentStatus.Completed, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(DocumentStatus.Completed, document.Status);
            Assert.NotNull(document.ProcessedAt);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonCompletedStatus_DoesNotSetProcessedAt()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.ProcessedAt = null;
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.UpdateStatusAsync(documentId, DocumentStatus.Processing, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Null(document.ProcessedAt);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithOptionalFields_UpdatesThem()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.UpdateStatusAsync(documentId, DocumentStatus.Completed, pageCount: 10, chunkCount: 25, content: "extracted text", cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(10, document.PageCount);
            Assert.Equal(25, document.ChunkCount);
            Assert.Equal("extracted text", document.Content);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNullOptionalFields_DoesNotOverwrite()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateSampleDocument(documentId);
            document.PageCount = 5;
            document.ChunkCount = 15;
            document.Content = "original content";
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            await _service.UpdateStatusAsync(documentId, DocumentStatus.Processing, cancellationToken: TestContext.Current.CancellationToken);

            // Assert
            Assert.Equal(5, document.PageCount);
            Assert.Equal(15, document.ChunkCount);
            Assert.Equal("original content", document.Content);
        }

        [Fact]
        public async Task UpdateStatusAsync_WithNonExistentDocument_ThrowsArgumentException()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentRepository.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns((Document?)null);

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(() =>
                _service.UpdateStatusAsync(documentId, DocumentStatus.Processing, cancellationToken: TestContext.Current.CancellationToken));
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
            await _fileStorageService.Received(1).DeleteFileAsync("/storage/test.pdf");
            await _documentChunkRepository.Received(1).DeleteByDocumentIdAsync(documentId);
            await _documentRepository.Received(1).DeleteAsync(documentId);
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
            await _fileStorageService.DidNotReceive().DeleteFileAsync(Arg.Any<string>());
            await _documentChunkRepository.Received(1).DeleteByDocumentIdAsync(documentId);
            await _documentRepository.Received(1).DeleteAsync(documentId);
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
            await _fileStorageService.DidNotReceive().DeleteFileAsync(Arg.Any<string>());
            await _documentChunkRepository.Received(1).DeleteByDocumentIdAsync(documentId);
            await _documentRepository.Received(1).DeleteAsync(documentId);
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

        #endregion
    }
}
