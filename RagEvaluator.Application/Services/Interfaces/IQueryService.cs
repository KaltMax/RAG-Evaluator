using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for query CRUD operations.
    /// </summary>
    public interface IQueryService
    {
        Task<QuerySummaryResponse?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync();
        Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel);
        Task CompleteQueryAsync(Query query, string answer, float[] queryEmbedding, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches);
        Task DeleteAsync(Guid id);
    }
}
