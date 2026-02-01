using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for query CRUD operations.
    /// </summary>
    public interface IQueryService
    {
        Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string embeddingModel, string chatModel);
        Task<QuerySummaryResponse?> GetByIdAsync(Guid id);
        Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync();
        Task DeleteAsync(Guid id);
    }
}
