using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Dtos.Notifications;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Runs a <see cref="DocumentReprocessingJob"/>: drives the document status lifecycle
    /// (Processing -> Completed, or Failed on error), broadcasts notifications, and delegates the
    /// actual work to <see cref="IDocumentService.ReprocessDocumentAsync"/>.
    /// </summary>
    public class DocumentReprocessingJobHandler : IJobHandler<DocumentReprocessingJob>
    {
        private readonly IDocumentService _documentService;
        private readonly IJobNotifier _jobNotifier;
        private readonly ILogger<DocumentReprocessingJobHandler> _logger;

        public DocumentReprocessingJobHandler(
            IDocumentService documentService,
            IJobNotifier jobNotifier,
            ILogger<DocumentReprocessingJobHandler> logger)
        {
            _documentService = documentService;
            _jobNotifier = jobNotifier;
            _logger = logger;
        }

        public async Task HandleAsync(DocumentReprocessingJob job, CancellationToken cancellationToken)
        {
            var documentId = job.DocumentId;

            var document = await _documentService.GetByIdAsync(documentId, cancellationToken);
            if (document is null)
            {
                _logger.LogError("Document {DocumentId} not found for reprocessing", documentId);
                return;
            }

            try
            {
                await _documentService.SetStatusAsync(documentId, DocumentStatus.Processing, cancellationToken);
                await NotifyAsync(documentId, DocumentStatus.Processing, document.FileName, cancellationToken);

                // ReprocessDocumentAsync rebuilds from stored content (throwing if there is none) and sets Completed.
                await _documentService.ReprocessDocumentAsync(documentId, cancellationToken);

                await NotifyAsync(documentId, DocumentStatus.Completed, document.FileName, cancellationToken);
                _logger.LogInformation("Document {DocumentId} reprocessed successfully", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to reprocess document {DocumentId}", documentId);

                await _documentService.SetStatusAsync(documentId, DocumentStatus.Failed, CancellationToken.None);
                await NotifyAsync(documentId, DocumentStatus.Failed, document.FileName, cancellationToken);
            }
        }

        private Task NotifyAsync(Guid documentId, DocumentStatus status, string fileName, CancellationToken cancellationToken)
        {
            return _jobNotifier.NotifyAsync(
                new JobNotification(JobTypes.Document, documentId, status.ToString(), fileName),
                cancellationToken);
        }
    }
}
