using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Mappers
{
    /// <summary>
    /// Extension methods for mapping Query entities to DTOs.
    /// </summary>
    public static class QueryMapper
    {
        public static QuerySummaryResponse ToSummary(this Query query)
        {
            return new QuerySummaryResponse
            {
                Id = query.Id,
                Question = query.Question,
                Language = query.Language,
                TopK = query.TopK,
                SystemPrompt = query.SystemPrompt,
                EmbeddingModel = query.EmbeddingModel,
                ChatModel = query.ChatModel,
                CreatedAt = query.CreatedAt,
                ResponseTimeMs = query.ResponseTimeMs,
                Mrr = query.MRR,
                PrecisionAtK = query.PrecisionAtK,
                RecallAtK = query.RecallAtK,
                NdcgAtK = query.NDCGAtK,
                ResponseQuality = query.ResponseQuality.HasValue ? (int)query.ResponseQuality.Value : null,
                HasLanguageSwitching = query.HasLanguageSwitching
            };
        }

        public static IReadOnlyList<QuerySummaryResponse> ToSummaryList(this IEnumerable<Query> queries)
        {
            return queries.Select(q => q.ToSummary()).ToList();
        }

        public static QueryResponse ToResponse(this Query query, string answer, IReadOnlyList<SearchResultDto> sources)
        {
            return new QueryResponse
            {
                QueryId = query.Id,
                Question = query.Question,
                Answer = answer,
                Sources = sources.ToList(),
                Timestamp = query.CreatedAt
            };
        }

        public static SearchResultDto ToSearchResultDto(this QueryResult result)
        {
            return new SearchResultDto
            {
                Id = result.Id,
                Text = result.ChunkText,
                Similarity = result.SimilarityScore,
                DocumentId = result.DocumentId,
                FileName = result.FileName,
                ChunkingStrategy = result.ChunkingStrategy,
                EmbeddingModel = result.EmbeddingModel
            };
        }

        public static IReadOnlyList<SearchResultDto> ToSearchResultDtoList(this IEnumerable<QueryResult> results)
        {
            return results
                .OrderBy(r => r.Rank)
                .Select(r => r.ToSearchResultDto())
                .ToList();
        }

        public static QueryResult ToQueryResult(this ChunkSearchMatch match, Guid queryId, int rank, double similarity)
        {
            return new QueryResult
            {
                Id = Guid.NewGuid(),
                QueryId = queryId,
                DocumentChunkId = match.Id,
                DocumentId = match.DocumentId,
                FileName = match.FileName,
                ChunkText = match.Text,
                ChunkingStrategy = match.ChunkingStrategy,
                EmbeddingModel = match.EmbeddingModel,
                Rank = rank,
                SimilarityScore = similarity
            };
        }
    }
}
