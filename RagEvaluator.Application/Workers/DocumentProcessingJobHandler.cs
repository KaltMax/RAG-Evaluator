using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;
using RagEvaluator.Contract.Dtos.Notifications;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Runs a <see cref="DocumentProcessingJob"/>: drives the document status lifecycle
    /// (Processing -> Completed, or Failed on error), broadcasts notifications, and delegates the
    /// actual work to <see cref="IDocumentService.ProcessDocumentAsync"/>.
    /// </summary>
    public class DocumentProcessingJobHandler : IJobHandler<DocumentProcessingJob>
    {
        private readonly IDocumentService _documentService;
        private readonly IJobNotifier _jobNotifier;
        private readonly ILogger<DocumentProcessingJobHandler> _logger;

        public DocumentProcessingJobHandler(
            IDocumentService documentService,
            IJobNotifier jobNotifier,
            ILogger<DocumentProcessingJobHandler> logger)
        {
            _documentService = documentService;
            _jobNotifier = jobNotifier;
            _logger = logger;
        }

        public async Task HandleAsync(DocumentProcessingJob job, CancellationToken cancellationToken)
        {
            var documentId = job.DocumentId;

            var fileInfo = await _documentService.GetDocumentFileInfoAsync(documentId, cancellationToken);
            if (fileInfo is null)
            {
                _logger.LogError("Document {DocumentId} not found for processing", documentId);
                return;
            }

            try
            {
                await _documentService.SetStatusAsync(documentId, DocumentStatus.Processing, cancellationToken);
                await NotifyAsync(documentId, DocumentStatus.Processing, fileInfo.FileName, cancellationToken);

                // ProcessDocumentAsync sets the document to Completed on success.
                await _documentService.ProcessDocumentAsync(documentId, fileInfo.FilePath, cancellationToken);

                await NotifyAsync(documentId, DocumentStatus.Completed, fileInfo.FileName, cancellationToken);
                _logger.LogInformation("Document {DocumentId} processed successfully", documentId);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process document {DocumentId}", documentId);

                await _documentService.SetStatusAsync(documentId, DocumentStatus.Failed, CancellationToken.None);
                await NotifyAsync(documentId, DocumentStatus.Failed, fileInfo.FileName, cancellationToken);
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
