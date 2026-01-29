using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Contract.Abstractions.Data
{
    /// <summary>
    /// Repository for document metadata and content persistence.
    /// </summary>
    public interface IDocumentRepository
    {
        Task<Document?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<Document>> GetAllAsync();

        /// <summary>
        /// Returns lightweight document summaries optimized for list views.
        /// </summary>
        Task<IReadOnlyList<DocumentSummary>> GetAllSummariesAsync();

        Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status);
        Task AddAsync(Document document);
        Task UpdateAsync(Document document);
        Task DeleteAsync(Guid id);
    }
}
