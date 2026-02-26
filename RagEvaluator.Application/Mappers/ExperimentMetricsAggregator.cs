using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Entities;

namespace RagEvaluator.Application.Mappers
{
    public static class ExperimentMetricsAggregator
    {
        public static ExperimentQueryGroupResponse BuildQueryGroup(
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

        public static ExperimentAggregatedMetrics ComputeAggregatedMetrics(List<Query> queries)
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
                MRR = mrrValues.Count > 0 ? new MetricAggregate { Mean = mrrValues.Average(), StdDev = ComputeStdDev(mrrValues) } : null,
                PrecisionAtK = precisionValues.Count > 0 ? new MetricAggregate { Mean = precisionValues.Average(), StdDev = ComputeStdDev(precisionValues) } : null,
                RecallAtK = recallValues.Count > 0 ? new MetricAggregate { Mean = recallValues.Average(), StdDev = ComputeStdDev(recallValues) } : null,
                NDCGAtK = ndcgValues.Count > 0 ? new MetricAggregate { Mean = ndcgValues.Average(), StdDev = ComputeStdDev(ndcgValues) } : null,
                ResponseQualityDistribution = qualityDistribution,
                LanguageSwitchingRate = languageSwitchingRate
            };
        }

        private static double ComputeStdDev(List<double> values)
        {
            if (values.Count <= 1)
            {
                return 0;
            }
            var mean = values.Average();
            var sumSquaredDiff = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSquaredDiff / (values.Count - 1));
        }
    }
}
