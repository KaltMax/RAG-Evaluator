using Microsoft.Extensions.Logging;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Application.Workers;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Dtos.Notifications;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Test.ApplicationTest
{
    public class DocumentProcessingJobHandlerTests
    {
        private readonly IDocumentService _documentService;
        private readonly IJobNotifier _jobNotifier;
        private readonly ILogger<DocumentProcessingJobHandler> _logger;
        private readonly DocumentProcessingJobHandler _handler;

        public DocumentProcessingJobHandlerTests()
        {
            _documentService = Substitute.For<IDocumentService>();
            _jobNotifier = Substitute.For<IJobNotifier>();
            _logger = Substitute.For<ILogger<DocumentProcessingJobHandler>>();
            _handler = new DocumentProcessingJobHandler(_documentService, _jobNotifier, _logger);
        }

        [Fact]
        public async Task HandleAsync_WithValidDocument_SetsProcessingThenProcessesAndNotifiesCompleted()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetDocumentFileInfoAsync(documentId, Arg.Any<CancellationToken>())
                .Returns(CreateFileInfo());

            // Act
            await _handler.HandleAsync(new DocumentProcessingJob(documentId), TestContext.Current.CancellationToken);

            // Assert
            await _documentService.Received(1).SetStatusAsync(documentId, DocumentStatus.Processing, Arg.Any<CancellationToken>());
            await _jobNotifier.Received(1).NotifyAsync(
                Arg.Is<JobNotification>(n => n.JobType == JobTypes.Document && n.Status == DocumentStatus.Processing.ToString()),
                Arg.Any<CancellationToken>());
            await _documentService.Received(1).ProcessDocumentAsync(documentId, "/storage/test.pdf", Arg.Any<CancellationToken>());
            await _jobNotifier.Received(1).NotifyAsync(
                Arg.Is<JobNotification>(n => n.JobType == JobTypes.Document && n.Status == DocumentStatus.Completed.ToString()),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_WhenProcessingFails_SetsFailedAndNotifies()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetDocumentFileInfoAsync(documentId, Arg.Any<CancellationToken>())
                .Returns(CreateFileInfo());
            _documentService.ProcessDocumentAsync(documentId, Arg.Any<string>(), Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("processing failed"));

            // Act
            await _handler.HandleAsync(new DocumentProcessingJob(documentId), TestContext.Current.CancellationToken);

            // Assert — failure persists via a set-based update and is broadcast
            await _documentService.Received(1).SetStatusAsync(documentId, DocumentStatus.Failed, Arg.Any<CancellationToken>());
            await _jobNotifier.Received(1).NotifyAsync(
                Arg.Is<JobNotification>(n => n.Status == DocumentStatus.Failed.ToString()),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_WhenDocumentNotFound_ReturnsWithoutProcessing()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetDocumentFileInfoAsync(documentId, Arg.Any<CancellationToken>())
                .Returns((DocumentFileInfo?)null);

            // Act
            await _handler.HandleAsync(new DocumentProcessingJob(documentId), TestContext.Current.CancellationToken);

            // Assert
            await _documentService.DidNotReceive().ProcessDocumentAsync(Arg.Any<Guid>(), Arg.Any<string>(), Arg.Any<CancellationToken>());
            await _documentService.DidNotReceive().SetStatusAsync(Arg.Any<Guid>(), Arg.Any<DocumentStatus>(), Arg.Any<CancellationToken>());
            await _jobNotifier.DidNotReceive().NotifyAsync(Arg.Any<JobNotification>(), Arg.Any<CancellationToken>());
        }

        private static DocumentFileInfo CreateFileInfo()
        {
            return new DocumentFileInfo
            {
                FilePath = "/storage/test.pdf",
                FileName = "test.pdf",
                MimeType = "application/pdf"
            };
        }
    }
}
