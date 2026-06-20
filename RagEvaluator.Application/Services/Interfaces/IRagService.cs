using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Orchestrates document ingestion (create + process).
    /// </summary>
    public interface IRagService
    {
        /// <summary>
        /// Processes a document by creating it, extracting content, generating chunks with embeddings, and storing them.
        /// </summary>
        Task<DocumentResponse> ProcessDocumentAsync(Stream documentStream, string fileName, string contentType, string language, string course, CancellationToken cancellationToken = default);
    }
}
