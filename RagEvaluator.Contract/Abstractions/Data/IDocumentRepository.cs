using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Contract.Abstractions.Data
{
    public interface IDocumentRepository
    {
        Task<Document?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<Document>> GetAllAsync();
        Task<IReadOnlyList<DocumentSummary>> GetAllSummariesAsync();
        Task<IReadOnlyList<Document>> GetByStatusAsync(DocumentStatus status);
        Task AddAsync(Document document);
        Task UpdateAsync(Document document);
        Task DeleteAsync(Guid id);
    }
}
