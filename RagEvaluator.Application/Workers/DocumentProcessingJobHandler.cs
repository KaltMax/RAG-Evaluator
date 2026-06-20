using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.BackgroundProcessing;

namespace RagEvaluator.Application.Workers
{
    /// <summary>
    /// Dispatches a <see cref="DocumentProcessingJob"/> to <see cref="IDocumentService.ProcessQueuedDocumentAsync"/>.
    /// </summary>
    public class DocumentProcessingJobHandler : IJobHandler<DocumentProcessingJob>
    {
        private readonly IDocumentService _documentService;

        public DocumentProcessingJobHandler(IDocumentService documentService)
        {
            _documentService = documentService;
        }

        public Task HandleAsync(DocumentProcessingJob job, CancellationToken cancellationToken)
        {
            return _documentService.ProcessQueuedDocumentAsync(job.DocumentId, cancellationToken);
        }
    }
}
