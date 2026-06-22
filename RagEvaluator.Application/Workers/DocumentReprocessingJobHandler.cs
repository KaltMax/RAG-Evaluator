using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Dispatches a <see cref="DocumentReprocessingJob"/> to <see cref="IDocumentService.ReprocessQueuedDocumentAsync"/>.
    /// </summary>
    public class DocumentReprocessingJobHandler : IJobHandler<DocumentReprocessingJob>
    {
        private readonly IDocumentService _documentService;

        public DocumentReprocessingJobHandler(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        public Task HandleAsync(DocumentReprocessingJob job, CancellationToken cancellationToken)
        {
            return _documentService.ReprocessQueuedDocumentAsync(job.DocumentId, cancellationToken);
        }
    }
}
