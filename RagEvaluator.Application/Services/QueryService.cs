using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Service for query CRUD operations.
    /// </summary>
    public class QueryService : IQueryService
    {
        private readonly IQueryRepository _queryRepository;

        public QueryService(IQueryRepository queryRepository)
        {
            _queryRepository = queryRepository;
        }

        public async Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string embeddingModel, string chatModel)
        {
            var query = new Query
            {
                Id = Guid.NewGuid(),
                Question = question,
                Language = language,
                TopK = topK,
                SystemPrompt = systemPrompt,
                EmbeddingModel = embeddingModel,
                ChatModel = chatModel,
                CreatedAt = DateTime.UtcNow
            };

            await _queryRepository.AddAsync(query);
            return query;
        }

        public async Task<QuerySummaryResponse?> GetByIdAsync(Guid id)
        {
            var query = await _queryRepository.GetByIdAsync(id);
            return query?.ToSummary();
        }

        public async Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync()
        {
            var queries = await _queryRepository.GetAllAsync();
            return queries.ToSummaryList();
        }

        public async Task DeleteAsync(Guid id)
        {
            await _queryRepository.DeleteAsync(id);
        }
    }
}
