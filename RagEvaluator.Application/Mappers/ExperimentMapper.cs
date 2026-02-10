using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Mappers
{
    /// <summary>
    /// Extension methods for mapping Experiment entities to DTOs.
    /// </summary>
    public static class ExperimentMapper
    {
        public static ExperimentSummaryResponse ToSummary(this Experiment experiment)
        {
            var annotatedCount = experiment.Queries.Count(q => q.ResponseQuality.HasValue);

            return new ExperimentSummaryResponse
            {
                Id = experiment.Id,
                Name = experiment.Name,
                Status = experiment.Status.ToString(),
                RepeatCount = experiment.RepeatCount,
                CreatedAt = experiment.CreatedAt,
                CompletedAt = experiment.CompletedAt,
                Progress = new ExperimentProgress
                {
                    Total = experiment.TotalQueryCount,
                    Completed = experiment.CompletedQueryCount,
                    Annotated = annotatedCount
                },
                EmbeddingModel = experiment.EmbeddingModel,
                ChunkingStrategy = experiment.ChunkingStrategy,
                PromptTemplate = experiment.PromptTemplate
            };
        }

        public static ExperimentResponse ToResponse(this Experiment experiment)
        {
            var queries = experiment.Queries.ToList();
            var annotatedCount = queries.Count(q => q.ResponseQuality.HasValue);

            var queryGroups = queries
                .GroupBy(q => new { q.Question, q.Language, q.TopK })
                .Select(g => ExperimentMetricsAggregator.BuildQueryGroup(g.Key.Question, g.Key.Language, g.Key.TopK, g.ToList()))
                .ToList();

            var allAnnotated = queries.Count > 0 && queries.All(q => q.ResponseQuality.HasValue);

            return new ExperimentResponse
            {
                Id = experiment.Id,
                Name = experiment.Name,
                Status = experiment.Status.ToString(),
                RepeatCount = experiment.RepeatCount,
                CreatedAt = experiment.CreatedAt,
                CompletedAt = experiment.CompletedAt,
                EmbeddingModel = experiment.EmbeddingModel,
                ChunkingStrategy = experiment.ChunkingStrategy,
                ChatModel = experiment.ChatModel,
                ChunkSize = experiment.ChunkSize,
                ChunkOverlap = experiment.ChunkOverlap,
                SimilarityThreshold = experiment.SimilarityThreshold,
                PromptTemplate = experiment.PromptTemplate,
                Progress = new ExperimentProgress
                {
                    Total = experiment.TotalQueryCount,
                    Completed = experiment.CompletedQueryCount,
                    Annotated = annotatedCount
                },
                QueryGroups = queryGroups,
                OverallMetrics = allAnnotated ? ExperimentMetricsAggregator.ComputeAggregatedMetrics(queries) : null
            };
        }
    }
}
