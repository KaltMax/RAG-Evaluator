using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Contract.Abstractions.Data
{
    public interface IDocumentChunkRepository
    {
        Task<DocumentChunk?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<DocumentChunk>> GetByDocumentIdAsync(Guid documentId);
        Task<int> GetCountAsync();
        Task AddAsync(DocumentChunk chunk);
        Task AddRangeAsync(IEnumerable<DocumentChunk> chunks);
        Task DeleteByDocumentIdAsync(Guid documentId);
        Task<IReadOnlyList<SearchResult>> SearchAsync(float[] queryEmbedding, int topK = 3);
    }
}
