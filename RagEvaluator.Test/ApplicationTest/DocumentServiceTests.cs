using Microsoft.Extensions.Logging;
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
        private readonly ILogger<DocumentService> _logger;
        private readonly IDocumentRepository _documentRepository;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly IFileStorageService _fileStorageService;
        private readonly DocumentService _service;

        public DocumentServiceTests()
        {
            _logger = Substitute.For<ILogger<DocumentService>>();
            _documentRepository = Substitute.For<IDocumentRepository>();
            _documentChunkRepository = Substitute.For<IDocumentChunkRepository>();
            _fileStorageService = Substitute.For<IFileStorageService>();
            _service = new DocumentService(_logger, _documentRepository, _documentChunkRepository, _fileStorageService);
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
            var result = await _service.CreateDocumentAsync(stream, "test.pdf", 1024, "application/pdf", "en", CancellationToken.None);

            // Assert
            Assert.Equal("test.pdf", result.FileName);
            Assert.Equal(expectedFilePath, result.FilePath);
            Assert.Equal(1024, result.FileSize);
            Assert.Equal("application/pdf", result.MimeType);
            Assert.Equal("en", result.Language);
            Assert.Equal(DocumentStatus.Pending, result.Status);
            await _fileStorageService.Received(1).SaveFileAsync(Arg.Any<Stream>(), result.Id, "test.pdf", Arg.Any<CancellationToken>());
            await _documentRepository.Received(1).AddAsync(result, Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task CreateDocumentAsync_WithPathInFileName_SanitizesFileName()
        {
            // Arrange
            var stream = new MemoryStream("PDF content"u8.ToArray());
            _fileStorageService.SaveFileAsync(Arg.Any<Stream>(), Arg.Any<Guid>(), "evil.pdf", Arg.Any<CancellationToken>())
                .Returns("/storage/some-guid.pdf");

            // Act
            var result = await _service.CreateDocumentAsync(stream, "../../../evil.pdf", 1024, "application/pdf", "en", CancellationToken.None);

            // Assert
            Assert.Equal("evil.pdf", result.FileName);
            await _fileStorageService.Received(1).SaveFileAsync(Arg.Any<Stream>(), result.Id, "evil.pdf", Arg.Any<CancellationToken>());
        }

        #endregion
    }
}
