using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for document processing operations including PDF text extraction, chunking, embedding, and chunk search.
    /// </summary>
    public interface IDocumentProcessingService
    {
        /// <summary>
        /// Processes a PDF document by extracting text, splitting into chunks, generating embeddings, and storing the chunks.
        /// </summary>
        Task ProcessDocumentContentAsync(Guid documentId, Stream pdfStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reprocesses all completed documents by deleting existing chunks and re-chunking + re-embedding with the current configuration.
        /// </summary>
        Task<ReprocessResponse> ReprocessAllDocumentsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all document chunks associated with a specific document.
        /// </summary>
        Task<IReadOnlyList<DocumentChunkResponse>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
