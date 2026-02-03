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
        Task<Document?> GetByIdAsync(Guid id);
        
        /// <summary>
        /// Gets all documents from the repository.
        /// </summary>
        Task<IReadOnlyList<Document>> GetAllAsync();

        /// <summary>
        /// Returns lightweight document summaries optimized for list views.
        /// </summary>
        Task<IReadOnlyList<DocumentSummary>> GetAllSummariesAsync();

        /// <summary>
        /// Gets all documents with a specific processing status.
        /// </summary>
        Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status);
        
        /// <summary>
        /// Adds a new document to the repository.
        /// </summary>
        Task AddAsync(Document document);
        
        /// <summary>
        /// Updates an existing document in the repository.
        /// </summary>
        Task UpdateAsync(Document document);
        
        /// <summary>
        /// Deletes a document by its unique identifier.
        /// </summary>
        Task DeleteAsync(Guid id);
    }
}
