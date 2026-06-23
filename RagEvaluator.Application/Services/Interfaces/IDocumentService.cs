using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Document feature service: upload, PDF processing, reprocessing, CRUD, and chunk retrieval.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Creates a Pending document, saves the file to storage, and enqueues it for background processing.
        /// </summary>
        Task<DocumentResponse> CreateDocumentAsync(Stream fileStream, string fileName, string mimeType, string language, string course, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a document by its unique identifier.
        /// </summary>
        Task<DocumentResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a document by its name.
        /// </summary>
        Task<DocumentResponse?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all document summaries optimized for list views.
        /// </summary>
        Task<IReadOnlyList<DocumentResponse>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets file information for a document including file path, name, and MIME type.
        /// </summary>
        Task<DocumentFileInfo?> GetDocumentFileInfoAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all document chunks associated with a specific document.
        /// </summary>
        Task<IReadOnlyList<DocumentChunkResponse>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a document, its associated file from storage, and all related chunks.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates a document's processing status.
        /// </summary>
        Task SetStatusAsync(Guid documentId, DocumentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a PDF document by extracting text, splitting into chunks, generating embeddings, and storing the chunks.
        /// Sets the document to Completed on success. Invoked by the background worker.
        /// </summary>
        Task ProcessDocumentAsync(Guid documentId, string filePath, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reprocesses a document by re-chunking and re-embedding its stored content and replacing its existing chunks.
        /// Sets the document to Completed on success. Invoked by the background worker.
        /// </summary>
        Task ReprocessDocumentAsync(Guid documentId, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reprocesses all documents with content by deleting existing chunks and re-chunking + re-embedding with the current configuration.
        /// </summary>
        Task<ReprocessResponse> ReprocessAllDocumentsAsync(CancellationToken cancellationToken = default);
    }
}
