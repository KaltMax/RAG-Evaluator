using Microsoft.AspNetCore.Http;
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
    /// Contains unit tests for the DocumentController class, validating the behavior of document upload, retrieval,
    /// deletion, and processing functionalities.
    /// </summary>
    public class DocumentControllerTests
    {
        private readonly ILogger<DocumentController> _logger;
        private readonly IRagService _ragService;
        private readonly IDocumentService _documentService;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly DocumentController _controller;

        public DocumentControllerTests()
        {
            _logger = Substitute.For<ILogger<DocumentController>>();
            _ragService = Substitute.For<IRagService>();
            _documentService = Substitute.For<IDocumentService>();
            _documentProcessingService = Substitute.For<IDocumentProcessingService>();
            _controller = new DocumentController(_logger, _ragService, _documentService, _documentProcessingService);
        }

        #region UploadDocumentAsync Tests

        [Fact]
        public async Task UploadDocumentAsync_WithInvalidModelState_ReturnsBadRequest()
        {
            // Arrange
            var formFile = CreateFormFile("test.pdf", "content"u8.ToArray(), "application/pdf");
            var request = new UploadDocumentRequest { File = formFile, Language = "es" };
            _controller.ModelState.AddModelError("Language", "Language must be 'en' or 'de'.");

            // Act
            var result = await _controller.UploadDocumentAsync(request, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            var modelState = Assert.IsAssignableFrom<SerializableError>(badRequestResult.Value);
            Assert.True(modelState.ContainsKey("Language"));
        }

        [Fact]
        public async Task UploadDocumentAsync_WithValidPdf_ReturnsOkWithDocument()
        {
            // Arrange
            var fileName = "test.pdf";
            var content = "PDF content"u8.ToArray();
            var formFile = CreateFormFile(fileName, content, "application/pdf");
            var request = new UploadDocumentRequest { File = formFile, Language = "en" };
            var expectedResponse = CreateDocumentResponse();

            _ragService.ProcessDocumentAsync(
                Arg.Any<Stream>(),
                fileName,
                "application/pdf",
                "en",
                Arg.Any<CancellationToken>())
                .Returns(expectedResponse);

            // Act
            var result = await _controller.UploadDocumentAsync(request, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DocumentResponse>(okResult.Value);
            Assert.Equal(expectedResponse.Id, response.Id);
            Assert.Equal(expectedResponse.FileName, response.FileName);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithEmptyFile_ReturnsBadRequest()
        {
            // Arrange
            var formFile = CreateFormFile("test.pdf", Array.Empty<byte>(), "application/pdf");
            var request = new UploadDocumentRequest { File = formFile, Language = "en" };

            // Act
            var result = await _controller.UploadDocumentAsync(request, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("No file uploaded.", badRequestResult.Value);
        }

        [Fact]
        public async Task UploadDocumentAsync_WithNonPdfFile_ReturnsBadRequest()
        {
            // Arrange
            var formFile = CreateFormFile("test.txt", "content"u8.ToArray(), "text/plain");
            var request = new UploadDocumentRequest { File = formFile, Language = "en" };

            // Act
            var result = await _controller.UploadDocumentAsync(request, TestContext.Current.CancellationToken);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("Only PDF files are supported.", badRequestResult.Value);
        }

        #endregion

        #region GetAllDocumentsAsync Tests

        [Fact]
        public async Task GetAllDocumentsAsync_ReturnsOkWithDocuments()
        {
            // Arrange
            var documents = new List<DocumentResponse>
            {
                CreateDocumentResponse(),
                CreateDocumentResponse()
            };
            _documentService.GetAllAsync(Arg.Any<CancellationToken>()).Returns(documents);

            // Act
            var result = await _controller.GetAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<DocumentResponse>>(okResult.Value);
            Assert.Equal(2, response.Count());
        }

        [Fact]
        public async Task GetAllDocumentsAsync_WhenNoDocuments_ReturnsEmptyList()
        {
            // Arrange
            _documentService.GetAllAsync(Arg.Any<CancellationToken>())
                .Returns(new List<DocumentResponse>());

            // Act
            var result = await _controller.GetAllDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<DocumentResponse>>(okResult.Value);
            Assert.Empty(response);
        }

        #endregion

        #region GetDocumentByIdAsync Tests

        [Fact]
        public async Task GetDocumentByIdAsync_WithValidId_ReturnsOkWithDocument()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateDocumentResponse(documentId);
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _controller.GetDocumentByIdAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<DocumentResponse>(okResult.Value);
            Assert.Equal(documentId, response.Id);
        }

        [Fact]
        public async Task GetDocumentByIdAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>())
                .Returns((DocumentResponse?)null);

            // Act
            var result = await _controller.GetDocumentByIdAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
        }

        #endregion

        #region DeleteDocumentAsync Tests

        [Fact]
        public async Task DeleteDocumentAsync_WithValidId_ReturnsNoContent()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateDocumentResponse(documentId);
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);

            // Act
            var result = await _controller.DeleteDocumentAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NoContentResult>(result);
            await _documentService.Received(1).DeleteAsync(documentId, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DeleteDocumentAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>())
                .Returns((DocumentResponse?)null);

            // Act
            var result = await _controller.DeleteDocumentAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            await _documentService.DidNotReceive().DeleteAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region DownloadDocumentAsync Tests

        [Fact]
        public async Task DownloadDocumentAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetDocumentFileInfoAsync(documentId, Arg.Any<CancellationToken>())
                .Returns((DocumentFileInfo?)null);

            // Act
            var result = await _controller.DownloadDocumentAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result);
            await _documentService.Received(1).GetDocumentFileInfoAsync(documentId, Arg.Any<CancellationToken>());
        }

        #endregion

        #region ReprocessDocumentsAsync Tests

        [Fact]
        public async Task ReprocessDocumentsAsync_ReturnsOkWithResponse()
        {
            // Arrange
            var expectedResponse = new ReprocessResponse
            {
                DocumentsProcessed = 5,
                TotalChunksCreated = 50,
                ChunkingStrategy = "FixedSize",
                EmbeddingModel = "nomic-embed-text-v2-moe"
            };
            _documentProcessingService.ReprocessAllDocumentsAsync(Arg.Any<CancellationToken>())
                .Returns(expectedResponse);

            // Act
            var result = await _controller.ReprocessDocumentsAsync(TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsType<ReprocessResponse>(okResult.Value);
            Assert.Equal(5, response.DocumentsProcessed);
            Assert.Equal(50, response.TotalChunksCreated);
        }

        #endregion

        #region GetDocumentChunksAsync Tests

        [Fact]
        public async Task GetDocumentChunksAsync_WithValidId_ReturnsOkWithChunks()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            var document = CreateDocumentResponse(documentId);
            var chunks = new List<DocumentChunkResponse>
            {
                new() { Id = Guid.NewGuid(), Text = "Chunk 1", ChunkingStrategy = "FixedSize", EmbeddingModel = "model", DocumentId = documentId },
                new() { Id = Guid.NewGuid(), Text = "Chunk 2", ChunkingStrategy = "FixedSize", EmbeddingModel = "model", DocumentId = documentId }
            };

            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(document);
            _documentProcessingService.GetChunksByDocumentIdAsync(documentId, Arg.Any<CancellationToken>()).Returns(chunks);

            // Act
            var result = await _controller.GetDocumentChunksAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result.Result);
            var response = Assert.IsAssignableFrom<IEnumerable<DocumentChunkResponse>>(okResult.Value);
            Assert.Equal(2, response.Count());
        }

        [Fact]
        public async Task GetDocumentChunksAsync_WithInvalidId_ReturnsNotFound()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>())
                .Returns((DocumentResponse?)null);

            // Act
            var result = await _controller.GetDocumentChunksAsync(documentId, TestContext.Current.CancellationToken);

            // Assert
            Assert.IsType<NotFoundResult>(result.Result);
            await _documentProcessingService.DidNotReceive()
                .GetChunksByDocumentIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
        }

        #endregion

        #region Helper Methods

        private FormFile CreateFormFile(string fileName, byte[] content, string contentType)
        {
            var stream = new MemoryStream(content);
            return new FormFile(stream, 0, content.Length, "file", fileName)
            {
                Headers = new HeaderDictionary(),
                ContentType = contentType
            };
        }

        private DocumentResponse CreateDocumentResponse(Guid? id = null)
        {
            return new DocumentResponse
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
                Status = "Completed"
            };
        }

        #endregion
    }
}
