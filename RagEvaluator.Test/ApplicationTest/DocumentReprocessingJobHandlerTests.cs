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
    public class DocumentReprocessingJobHandlerTests
    {
        private readonly IDocumentService _documentService;
        private readonly IJobNotifier _jobNotifier;
        private readonly ILogger<DocumentReprocessingJobHandler> _logger;
        private readonly DocumentReprocessingJobHandler _handler;

        public DocumentReprocessingJobHandlerTests()
        {
            _documentService = Substitute.For<IDocumentService>();
            _jobNotifier = Substitute.For<IJobNotifier>();
            _logger = Substitute.For<ILogger<DocumentReprocessingJobHandler>>();
            _handler = new DocumentReprocessingJobHandler(_documentService, _jobNotifier, _logger);
        }

        [Fact]
        public async Task HandleAsync_WithValidDocument_SetsProcessingThenReprocessesAndNotifiesCompleted()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>())
                .Returns(CreateResponse(documentId));

            // Act
            await _handler.HandleAsync(new DocumentReprocessingJob(documentId), TestContext.Current.CancellationToken);

            // Assert
            await _documentService.Received(1).SetStatusAsync(documentId, DocumentStatus.Processing, Arg.Any<CancellationToken>());
            await _jobNotifier.Received(1).NotifyAsync(
                Arg.Is<JobNotification>(n => n.JobType == JobTypes.Document && n.Status == DocumentStatus.Processing.ToString()),
                Arg.Any<CancellationToken>());
            await _documentService.Received(1).ReprocessDocumentAsync(documentId, Arg.Any<CancellationToken>());
            await _jobNotifier.Received(1).NotifyAsync(
                Arg.Is<JobNotification>(n => n.JobType == JobTypes.Document && n.Status == DocumentStatus.Completed.ToString()),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task HandleAsync_WhenProcessingFails_SetsFailedAndNotifies()
        {
            // Arrange
            var documentId = Guid.NewGuid();
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>())
                .Returns(CreateResponse(documentId));
            _documentService.ReprocessDocumentAsync(documentId, Arg.Any<CancellationToken>())
                .ThrowsAsync(new InvalidOperationException("reprocessing failed"));

            // Act
            await _handler.HandleAsync(new DocumentReprocessingJob(documentId), TestContext.Current.CancellationToken);

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
            _documentService.GetByIdAsync(documentId, Arg.Any<CancellationToken>())
                .Returns((DocumentResponse?)null);

            // Act
            await _handler.HandleAsync(new DocumentReprocessingJob(documentId), TestContext.Current.CancellationToken);

            // Assert
            await _documentService.DidNotReceive().ReprocessDocumentAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>());
            await _documentService.DidNotReceive().SetStatusAsync(Arg.Any<Guid>(), Arg.Any<DocumentStatus>(), Arg.Any<CancellationToken>());
            await _jobNotifier.DidNotReceive().NotifyAsync(Arg.Any<JobNotification>(), Arg.Any<CancellationToken>());
        }

        private static DocumentResponse CreateResponse(Guid id)
        {
            return new DocumentResponse
            {
                Id = id,
                FileName = "test.pdf",
                Course = "Test Course",
                Status = DocumentStatus.Completed.ToString()
            };
        }
    }
}
