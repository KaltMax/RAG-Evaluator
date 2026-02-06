using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
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

        public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            return await _embeddingService.IsAvailableAsync(cancellationToken);
        }

        public async Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel, CancellationToken cancellationToken = default)
        {
            var embedding = await _embeddingService.GenerateEmbeddingAsync($"search_query: {question}", cancellationToken);

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

        public async Task CompleteQueryAsync(Query query, string answer, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches, CancellationToken cancellationToken = default)
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

            await _queryRepository.AddAsync(query, cancellationToken);
        }

        public async Task<QuerySummaryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var query = await _queryRepository.GetByIdAsync(id, cancellationToken);
            return query?.ToSummary();
        }

        public async Task<IReadOnlyList<QuerySummaryResponse>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var queries = await _queryRepository.GetAllAsync(cancellationToken);
            return queries.ToSummaryList();
        }

        public async Task AnnotateResultsAsync(Guid queryId, IEnumerable<QueryResultAnnotation> annotations, ResponseQuality responseQuality, bool hasLanguageSwitching, IEnumerable<Guid> relevantDocumentIds, CancellationToken cancellationToken = default)
        {
            var query = await _queryRepository.GetByIdWithResultsAsync(queryId, cancellationToken);
            if (query == null)
            {
                throw new ArgumentException($"Query with id {queryId} not found");
            }

            var resultsDictionary = query.Results.ToDictionary(r => r.Id);

            foreach (var annotation in annotations)
            {
                if (resultsDictionary.TryGetValue(annotation.ResultId, out var result))
                {
                    result.RelevanceGrade = annotation.RelevanceGrade;
                    result.IsRelevant = annotation.RelevanceGrade != RelevanceGrade.NotRelevant;
                }
            }

            query.ResponseQuality = responseQuality;
            query.HasLanguageSwitching = hasLanguageSwitching;

            // Update ground truth relevant documents for Recall@K calculation
            query.RelevantDocuments.Clear();
            foreach (var documentId in relevantDocumentIds.Distinct())
            {
                query.RelevantDocuments.Add(new QueryRelevantDocument
                {
                    QueryId = queryId,
                    DocumentId = documentId
                });
            }

            CalculateMetrics(query);
            await _queryRepository.UpdateAsync(query, cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _queryRepository.DeleteAsync(id, cancellationToken);
        }

        private void CalculateMetrics(Query query)
        {
            // Check if any results have been labeled
            var hasLabels = query.Results.Any(r => r.IsRelevant.HasValue);
            if (!hasLabels)
            {
                return; // No relevance labels, nothing to calculate
            }

            var groundTruthDocumentIds = query.RelevantDocuments.Select(rd => rd.DocumentId).ToList();
            var metrics = _metricsService.CalculateQueryMetrics(query.Results.ToList(), query.TopK, groundTruthDocumentIds);

            query.MRR = metrics.MRR;
            query.PrecisionAtK = metrics.PrecisionAtK;
            query.RecallAtK = metrics.RecallAtK;
            query.NDCGAtK = metrics.NDCGAtK;
        }
    }
}
