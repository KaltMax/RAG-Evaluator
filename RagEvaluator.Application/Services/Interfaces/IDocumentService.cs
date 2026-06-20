using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Document feature service: upload, PDF processing, reprocessing, CRUD, and chunk retrieval.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Uploads a document: creates it, saves the file, then extracts content, chunks, embeds, and stores it.
        /// </summary>
        Task<DocumentResponse> UploadDocumentAsync(Stream documentStream, string fileName, string contentType, string language, string course, CancellationToken cancellationToken = default);

        /// <summary>
        /// Creates a new document entity and saves the file to storage.
        /// </summary>
        Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long fileSize, string mimeType, string language, string course, CancellationToken cancellationToken = default);

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
        /// Updates the processing status of a document, stamping ProcessedAt when it becomes Completed.
        /// </summary>
        Task UpdateStatusAsync(Guid id, DocumentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a document, its associated file from storage, and all related chunks.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Processes a PDF document by extracting text, splitting into chunks, generating embeddings, and storing the chunks.
        /// </summary>
        Task ProcessDocumentContentAsync(Guid documentId, Stream pdfStream, CancellationToken cancellationToken = default);

        /// <summary>
        /// Reprocesses all documents with content by deleting existing chunks and re-chunking + re-embedding with the current configuration.
        /// </summary>
        Task<ReprocessResponse> ReprocessAllDocumentsAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all document chunks associated with a specific document.
        /// </summary>
        Task<IReadOnlyList<DocumentChunkResponse>> GetChunksByDocumentIdAsync(Guid documentId, CancellationToken cancellationToken = default);
    }
}
