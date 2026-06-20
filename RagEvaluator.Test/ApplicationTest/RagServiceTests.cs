using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RagEvaluator.Application.Services;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class RagServiceTests
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentProcessingService _documentProcessingService;
        private readonly RagService _service;

        public RagServiceTests()
        {
            _documentService = Substitute.For<IDocumentService>();
            _documentProcessingService = Substitute.For<IDocumentProcessingService>();
            _service = new RagService(_documentService, _documentProcessingService);
        }

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
    }
}
