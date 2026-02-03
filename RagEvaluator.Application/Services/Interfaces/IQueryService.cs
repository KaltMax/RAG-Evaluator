using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for query operations including creation, embedding generation, and persistence.
    /// </summary>
    public interface IQueryService
    {
        Task<bool> IsReadyAsync();
        Task<QuerySummaryResponse?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync();
        Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel);
        Task CompleteQueryAsync(Query query, string answer, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches);
        Task CalculateMetricsAsync(Guid queryId);
        Task DeleteAsync(Guid id);
    }
}
