using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Mappers
{
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
                .Select(g => BuildQueryGroup(g.Key.Question, g.Key.Language, g.Key.TopK, g.ToList()))
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
                OverallMetrics = allAnnotated ? ComputeAggregatedMetrics(queries) : null
            };
        }

        private static ExperimentQueryGroupResponse BuildQueryGroup(
            string question, string language, int topK, List<Query> queries)
        {
            var annotatedQueries = queries.Where(q => q.ResponseQuality.HasValue).ToList();

            return new ExperimentQueryGroupResponse
            {
                Question = question,
                Language = language,
                TopK = topK,
                QueryIds = queries.Select(q => q.Id).ToList(),
                AnnotatedCount = annotatedQueries.Count,
                Metrics = annotatedQueries.Count > 0 ? ComputeAggregatedMetrics(annotatedQueries) : null
            };
        }

        private static ExperimentAggregatedMetrics ComputeAggregatedMetrics(List<Query> queries)
        {
            var responseTimes = queries.Select(q => (double)q.ResponseTimeMs).ToList();

            var mrrValues = queries.Where(q => q.MRR.HasValue).Select(q => q.MRR!.Value).ToList();
            var precisionValues = queries.Where(q => q.PrecisionAtK.HasValue).Select(q => q.PrecisionAtK!.Value).ToList();
            var recallValues = queries.Where(q => q.RecallAtK.HasValue).Select(q => q.RecallAtK!.Value).ToList();
            var ndcgValues = queries.Where(q => q.NDCGAtK.HasValue).Select(q => q.NDCGAtK!.Value).ToList();

            Dictionary<string, int>? qualityDistribution = null;
            var qualityValues = queries.Where(q => q.ResponseQuality.HasValue).ToList();
            if (qualityValues.Count > 0)
            {
                qualityDistribution = qualityValues
                    .GroupBy(q => q.ResponseQuality!.Value)
                    .ToDictionary(g => g.Key.ToString(), g => g.Count());
            }

            double? languageSwitchingRate = null;
            var switchingValues = queries.Where(q => q.HasLanguageSwitching.HasValue).ToList();
            if (switchingValues.Count > 0)
            {
                languageSwitchingRate = (double)switchingValues.Count(q => q.HasLanguageSwitching!.Value) / switchingValues.Count;
            }

            return new ExperimentAggregatedMetrics
            {
                ResponseTimeMs = new MetricAggregate
                {
                    Mean = responseTimes.Average(),
                    StdDev = ComputeStdDev(responseTimes)
                },
                MeanMRR = mrrValues.Count > 0 ? mrrValues.Average() : null,
                MeanPrecisionAtK = precisionValues.Count > 0 ? precisionValues.Average() : null,
                MeanRecallAtK = recallValues.Count > 0 ? recallValues.Average() : null,
                MeanNDCGAtK = ndcgValues.Count > 0 ? ndcgValues.Average() : null,
                ResponseQualityDistribution = qualityDistribution,
                LanguageSwitchingRate = languageSwitchingRate
            };
        }

        private static double ComputeStdDev(List<double> values)
        {
            if (values.Count <= 1) return 0;
            var mean = values.Average();
            var sumSquaredDiff = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSquaredDiff / (values.Count - 1));
        }
    }
}
