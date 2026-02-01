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
                CreatedAt = query.CreatedAt
            };
        }

        public static IReadOnlyList<QuerySummaryResponse> ToSummaryList(this IEnumerable<Query> queries)
        {
            return queries.Select(q => q.ToSummary()).ToList();
        }

        public static QueryResponse ToResponse(this Query query, string answer, List<SearchResultDto> sources)
        {
            return new QueryResponse
            {
                QueryId = query.Id,
                Question = query.Question,
                Answer = answer,
                Sources = sources
            };
        }

        public static SearchResultDto ToSearchResultDto(this ChunkSearchMatch match, double similarity)
        {
            return new SearchResultDto
            {
                Id = match.Id,
                Text = match.Text,
                Similarity = similarity,
                DocumentId = match.DocumentId,
                FileName = match.FileName,
                ChunkingStrategy = match.ChunkingStrategy,
                EmbeddingModel = match.EmbeddingModel
            };
        }

        public static List<SearchResultDto> ToSearchResultDtoList(
            this IEnumerable<ChunkSearchMatch> matches,
            float[] questionEmbedding,
            Func<float[], float[], double> similarityFunc)
        {
            return matches
                .Select(m => m.ToSearchResultDto(similarityFunc(questionEmbedding, m.Embedding)))
                .ToList();
        }
    }
}
