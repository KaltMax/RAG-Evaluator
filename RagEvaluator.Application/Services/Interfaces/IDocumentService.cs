using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for document CRUD operations.
    /// </summary>
    public interface IDocumentService
    {
        /// <summary>
        /// Creates a new document entity and saves the file to storage.
        /// </summary>
        Task<Document> CreateDocumentAsync(Stream fileStream, string fileName, long fileSize, string mimeType, string language, string? course = null, CancellationToken cancellationToken = default);

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
    }
}
