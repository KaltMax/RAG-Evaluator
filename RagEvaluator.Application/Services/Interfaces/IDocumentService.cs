using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for document and chunk operations including PDF processing, chunking, and embedding.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Creates a new document entity and saves the file to storage.
        /// </summary>
        Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long? fileSize, string? mimeType, string language, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a document by its unique identifier.
        /// </summary>
        Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all document summaries optimized for list views.
        /// </summary>
        Task<IReadOnlyList<DocumentResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information for a document including file path, name, and MIME type.
        /// </summary>
        Task<DocumentFileInfo?> GetDocumentFileInfoAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates the processing status of a document and optionally updates page count, chunk count, and content.
        /// </summary>
        Task UpdateStatusAsync(Guid id, DocumentStatus status, int? pageCount = null, int? chunkCount = null, string? content = null, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a document, its associated file from storage, and all related chunks.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

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

        /// <summary>
        /// Searches for similar document chunks using vector similarity with the provided query embedding.
        /// </summary>
        Task<IReadOnlyList<ChunkSearchMatch>> SearchChunksAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken = default);
    }
}
