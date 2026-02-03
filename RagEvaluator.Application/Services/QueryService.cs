using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
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
        private readonly IEmbeddingService _embeddingService;
        private readonly IQueryRepository _queryRepository;
        private readonly IMetricsService _metricsService;

        public QueryService(IEmbeddingService embeddingService, IQueryRepository queryRepository, IMetricsService metricsService)
        {
            _embeddingService = embeddingService;
            _queryRepository = queryRepository;
            _metricsService = metricsService;
        }

        public async Task<bool> IsReadyAsync()
        {
            return await _embeddingService.IsAvailableAsync();
        }

        public async Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync($"search_query: {question}");

            return new Query
            {
                Id = Guid.NewGuid(),
                Question = question,
                Language = language,
                TopK = topK,
                SystemPrompt = systemPrompt,
                ChunkingStrategy = chunkingStrategy,
                EmbeddingModel = embeddingModel,
                ChatModel = chatModel,
                CreatedAt = DateTime.UtcNow,
                QueryEmbedding = embedding
            };
        }

        public async Task CompleteQueryAsync(Query query, string answer, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches)
        {
            query.Answer = answer;
            query.ResponseTimeMs = responseTimeMs;

            // Create QueryResult entities from chunk matches
            var rank = 1;
            foreach (var match in chunkMatches)
            {
                var similarity = _metricsService.CosineSimilarity(query.QueryEmbedding, match.Embedding);
                var result = match.ToQueryResult(query.Id, rank++, similarity);
                query.Results.Add(result);
            }

            await _queryRepository.AddAsync(query);
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
