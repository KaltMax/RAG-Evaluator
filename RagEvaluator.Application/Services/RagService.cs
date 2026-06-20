using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Orchestrates document ingestion (create + process).
    /// </summary>
    public class RagService : IRagService
    {
        private readonly IDocumentService _documentService;
        private readonly IDocumentProcessingService _documentProcessingService;

        public RagService(
            IDocumentService documentService,
            IDocumentProcessingService documentProcessingService)
        {
            _documentService = documentService;
            _documentProcessingService = documentProcessingService;
        }

        public async Task<DocumentResponse> ProcessDocumentAsync(Stream documentStream, string fileName, string contentType, string language, string course, CancellationToken cancellationToken = default)
        {
            // Create document with Pending status
            var document = await _documentService.CreateDocumentAsync(documentStream, fileName, documentStream.Length, contentType, language, course, cancellationToken);

            try
            {
                // Update status to Processing
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Processing, cancellationToken: cancellationToken);

                // Process document content (PDF → chunks → embeddings → store → Completed)
                documentStream.Position = 0;
                await _documentProcessingService.ProcessDocumentContentAsync(document.Id, documentStream, cancellationToken);

                // Return updated document
                var updatedDocument = await _documentService.GetByIdAsync(document.Id, cancellationToken);
                return updatedDocument!;
            }
            catch
            {
                // Update status to Failed on error
                await _documentService.UpdateStatusAsync(document.Id, DocumentStatus.Failed);
                throw;
            }
        }
    }
}
