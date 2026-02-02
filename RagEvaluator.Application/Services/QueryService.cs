using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Service for query CRUD operations.
    /// </summary>
    public class QueryService : IQueryService
    {
        private readonly IQueryRepository _queryRepository;
        private readonly IMetricsService _metricsService;

        public QueryService(IQueryRepository queryRepository, IMetricsService metricsService)
        {
            _queryRepository = queryRepository;
            _metricsService = metricsService;
        }

        public async Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel)
        {
            var query = new Query
            {
                Id = Guid.NewGuid(),
                Question = question,
                Language = language,
                TopK = topK,
                SystemPrompt = systemPrompt,
                ChunkingStrategy = chunkingStrategy,
                EmbeddingModel = embeddingModel,
                ChatModel = chatModel,
                CreatedAt = DateTime.UtcNow
            };

            await _queryRepository.AddAsync(query);
            return query;
        }

        public async Task CompleteQueryAsync(Query query, string answer, float[] queryEmbedding, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches)
        {
            query.Answer = answer;
            query.QueryEmbedding = queryEmbedding;
            query.ResponseTimeMs = responseTimeMs;

            // Create QueryResult entities from chunk matches
            var rank = 1;
            foreach (var match in chunkMatches)
            {
                var similarity = _metricsService.CosineSimilarity(queryEmbedding, match.Embedding);
                var result = match.ToQueryResult(query.Id, rank++, similarity);
                query.Results.Add(result);
            }

            await _queryRepository.UpdateAsync(query);
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
