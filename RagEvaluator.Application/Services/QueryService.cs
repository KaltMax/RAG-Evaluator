using System.Diagnostics;
using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Data;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.Enums;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Query feature service: runs the RAG question-answering pipeline and manages query
    /// persistence, history, annotation, and deletion.
    /// </summary>
    public class QueryService : IQueryService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IQueryRepository _queryRepository;
        private readonly IMetricsService _metricsService;
        private readonly IChatService _chatService;
        private readonly IDocumentChunkRepository _documentChunkRepository;
        private readonly RagConfiguration _config;

        public QueryService(
            IEmbeddingService embeddingService,
            IQueryRepository queryRepository,
            IMetricsService metricsService,
            IChatService chatService,
            IDocumentChunkRepository documentChunkRepository,
            RagConfiguration config)
        {
            _embeddingService = embeddingService;
            _queryRepository = queryRepository;
            _metricsService = metricsService;
            _chatService = chatService;
            _documentChunkRepository = documentChunkRepository;
            _config = config;
        }

        public async Task<QueryResponse> AskQuestionAsync(AskQuestionRequest request, CancellationToken cancellationToken = default)
        {
            if (!await _chatService.IsAvailableAsync(cancellationToken) || !await IsReadyAsync(cancellationToken))
            {
                throw new InvalidOperationException("RAG services not available. Ensure Ollama is running with the required models.");
            }

            var stopwatch = Stopwatch.StartNew();

            // Resolve prompt from template + query language
            var systemPrompt = PromptTemplateResolver.Resolve(_config.PromptTemplate, request.Language, _config);

            // Create query object with configuration snapshot
            var query = await CreateQueryAsync(
                request.Question,
                request.Language,
                request.TopK,
                systemPrompt,
                _config.ChunkingStrategy.ToString(),
                _config.EmbeddingModel,
                _config.ChatModel,
                cancellationToken);

            // Search for relevant document chunks
            var chunkMatches = await SearchChunksAsync(query.QueryEmbedding, query.TopK, cancellationToken);

            string answer;
            if (chunkMatches.Count == 0)
            {
                answer = "No relevant documents found in the knowledge base. Please upload documents first.";
            }
            else
            {
                // Build context from search results
                var context = string.Join("\n\n", chunkMatches.Select(r => r.Text));

                // Generate answer using LLM
                var userMessage = $"Context:\n{context}\n\nQuestion: {request.Question}\n\nAnswer:";
                answer = await _chatService.GenerateResponseAsync(systemPrompt, userMessage, cancellationToken);
            }

            // Calculate response time
            stopwatch.Stop();
            var responseTimeMs = (int)stopwatch.ElapsedMilliseconds;

            // Persist query results (answer, retrieved chunks)
            await CompleteQueryAsync(query, answer, responseTimeMs, chunkMatches, cancellationToken);

            return query.ToResponse();
        }

        public async Task<QueryResponse?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var query = await _queryRepository.GetByIdWithResultsAsync(id, cancellationToken);
            return query?.ToResponse();
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
            await PropagateChunkAnnotationsToSiblings(query, cancellationToken);
        }

        public async Task DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            await _queryRepository.DeleteAsync(id, cancellationToken);
        }

        private async Task<bool> IsReadyAsync(CancellationToken cancellationToken)
        {
            return await _embeddingService.IsAvailableAsync(cancellationToken);
        }

        private async Task<Query> CreateQueryAsync(string question, string language, int topK, string systemPrompt, string chunkingStrategy, string embeddingModel, string chatModel, CancellationToken cancellationToken)
        {
            var embedding = await _embeddingService.GenerateQueryEmbeddingAsync(question, cancellationToken);

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

        private async Task CompleteQueryAsync(Query query, string answer, int responseTimeMs, IEnumerable<ChunkSearchMatch> chunkMatches, CancellationToken cancellationToken)
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

        private async Task<IReadOnlyList<ChunkSearchMatch>> SearchChunksAsync(float[] queryEmbedding, int topK, CancellationToken cancellationToken)
        {
            return await _documentChunkRepository.SearchAsync(queryEmbedding, topK, cancellationToken);
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

        private async Task PropagateChunkAnnotationsToSiblings(Query query, CancellationToken cancellationToken)
        {
            if (query.ExperimentId.HasValue)
            {
                // Group by chunk text: identical chunks can appear more than once in a result set,
                // and siblings are matched by text (their result IDs differ across repeats).
                var siblingGrades = query.Results
                    .Where(r => r.RelevanceGrade.HasValue)
                    .GroupBy(r => r.ChunkText)
                    .ToDictionary(g => g.Key, g => g.First().RelevanceGrade!.Value);

                var siblings = await _queryRepository.GetUnannotatedSiblingsAsync(
                    query.Id, query.ExperimentId.Value, query.Question, query.Language, query.TopK, cancellationToken);

                foreach (var sibling in siblings)
                {
                    foreach (var result in sibling.Results)
                    {
                        if (!result.RelevanceGrade.HasValue && siblingGrades.TryGetValue(result.ChunkText, out var grade))
                        {
                            result.RelevanceGrade = grade;
                            result.IsRelevant = grade != RelevanceGrade.NotRelevant;
                        }
                    }

                    await _queryRepository.UpdateAsync(sibling, cancellationToken);
                }
            }
        }
    }
}
