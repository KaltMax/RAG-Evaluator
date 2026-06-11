using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Abstractions.Data
{
    /// <summary>
    /// Repository for document metadata and content persistence.
    /// </summary>
    public interface IDocumentRepository
    {
        /// <summary>
        /// Gets a document by its unique identifier.
        /// </summary>
        Task<Document?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets a document by its name.
        /// </summary>
        Task<Document?> GetByNameAsync(string name, CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all documents from the repository.
        /// </summary>
        Task<IReadOnlyList<Document>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Returns lightweight document summaries optimized for list views.
        /// </summary>
        Task<IReadOnlyList<DocumentSummary>> GetAllSummariesAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Gets all documents with a specific processing status.
        /// </summary>
        Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Adds a new document to the repository.
        /// </summary>
        Task AddAsync(Document document, CancellationToken cancellationToken = default);

        /// <summary>
        /// Updates an existing document in the repository.
        /// </summary>
        Task UpdateAsync(Document document, CancellationToken cancellationToken = default);

        /// <summary>
        /// Sets the status of the given documents.
        /// </summary>
        Task SetStatusAsync(IEnumerable<Guid> documentIds, DocumentStatus status, CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes a document by its unique identifier.
        /// </summary>
        Task DeleteAsync(Guid id, CancellationToken cancellationToken = default);
    }
}
