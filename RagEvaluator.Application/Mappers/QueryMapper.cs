using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

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
    }
}
