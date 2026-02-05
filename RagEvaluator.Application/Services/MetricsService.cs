using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Service for calculating retrieval and similarity metrics
    /// </summary>
    public class MetricsService : IMetricsService
    {
        public double CosineSimilarity(float[] a, float[] b)
        {
            return 1.0 - CosineDistance(a, b);
        }

        public double CosineDistance(float[] a, float[] b)
        {
            if (a.Length != b.Length)
            {
                return 1.0;
            }

            double dotProduct = 0;
            double magnitudeA = 0;
            double magnitudeB = 0;

            for (int i = 0; i < a.Length; i++)
            {
                dotProduct += a[i] * b[i];
                magnitudeA += a[i] * a[i];
                magnitudeB += b[i] * b[i];
            }

            magnitudeA = Math.Sqrt(magnitudeA);
            magnitudeB = Math.Sqrt(magnitudeB);

            if (magnitudeA == 0 || magnitudeB == 0)
            {
                return 1.0;
            }

            var similarity = dotProduct / (magnitudeA * magnitudeB);
            return 1.0 - similarity;
        }

        public double MeanReciprocalRank(IReadOnlyList<int?> relevantRanks)
        {
            if (relevantRanks.Count == 0)
            {
                return 0.0;
            }

            double sum = 0.0;
            foreach (var rank in relevantRanks)
            {
                if (rank.HasValue && rank.Value > 0)
                {
                    sum += 1.0 / rank.Value;
                }
            }

            return sum / relevantRanks.Count;
        }

        public double PrecisionAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k)
        {
            if (k <= 0 || retrievedIds.Count == 0)
            {
                return 0.0;
            }

            var relevantSet = new HashSet<string>(relevantIds);
            var topK = retrievedIds.Take(k).ToList();

            if (topK.Count == 0)
            {
                return 0.0;
            }

            int relevantInTopK = topK.Count(id => relevantSet.Contains(id));
            return (double)relevantInTopK / topK.Count;
        }

        public double RecallAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k)
        {
            if (k <= 0 || relevantIds.Count == 0)
            {
                return 0.0;
            }

            var relevantSet = new HashSet<string>(relevantIds);
            var topK = retrievedIds.Take(k).ToList();

            int relevantInTopK = topK.Count(id => relevantSet.Contains(id));
            return (double)relevantInTopK / relevantIds.Count;
        }

        public double NormalizedDiscountedCumulativeGainAtK(IReadOnlyList<double> relevanceScores, int k)
        {
            if (k <= 0 || relevanceScores.Count == 0)
            {
                return 0.0;
            }

            var scores = relevanceScores.Take(k).ToList();

            // Calculate DCG
            double dcg = 0.0;
            for (int i = 0; i < scores.Count; i++)
            {
                // Using the formula: rel_i / log2(i + 2) where i is 0-indexed
                dcg += scores[i] / Math.Log2(i + 2);
            }

            // Calculate IDCG (ideal DCG with scores sorted in descending order)
            var idealScores = scores.OrderByDescending(s => s).ToList();
            double idcg = 0.0;
            for (int i = 0; i < idealScores.Count; i++)
            {
                idcg += idealScores[i] / Math.Log2(i + 2);
            }

            if (idcg == 0.0)
            {
                return 0.0;
            }

            return dcg / idcg;
        }

        public QueryMetrics CalculateQueryMetrics(IReadOnlyList<QueryResult> results, int topK, IReadOnlyList<Guid> groundTruthDocumentIds)
        {
            // Order results by rank for metric calculations
            var orderedResults = results.OrderBy(r => r.Rank).ToList();

            // Calculate MRR: 1/rank of first relevant result (0 if none)
            var firstRelevantRank = orderedResults
                .Where(r => r.IsRelevant == true)
                .Select(r => (int?)r.Rank)
                .FirstOrDefault();
            var mrr = MeanReciprocalRank([firstRelevantRank]);

            // Calculate Precision@K (chunk-level)
            var retrievedChunkIds = orderedResults
                .Select(r => r.DocumentChunkId.ToString())
                .ToList();
            var relevantChunkIds = orderedResults
                .Where(r => r.IsRelevant == true)
                .Select(r => r.DocumentChunkId.ToString())
                .ToList();

            var precisionAtK = PrecisionAtK(retrievedChunkIds, relevantChunkIds, topK);

            // Calculate Recall@K (document-level) using ground truth
            // Formula: (# relevant documents with chunks in top K) / (# total relevant documents from ground truth)
            var relevantDocsFoundInTopK = orderedResults
                .Take(topK)
                .Where(r => r.IsRelevant == true)
                .Select(r => r.DocumentId)
                .Distinct()
                .ToList();

            var recallAtK = groundTruthDocumentIds.Count > 0
                ? (double)relevantDocsFoundInTopK.Count(id => groundTruthDocumentIds.Contains(id)) / groundTruthDocumentIds.Count
                : 0.0;

            // Calculate NDCG@K using RelevanceGrade if available, otherwise binary (1.0 for relevant, 0.0 for not)
            var relevanceScores = orderedResults
                .Select(r => r.RelevanceGrade.HasValue
                    ? (double)(int)r.RelevanceGrade.Value
                    : (r.IsRelevant == true ? 1.0 : 0.0))
                .ToList();
            var ndcgAtK = NormalizedDiscountedCumulativeGainAtK(relevanceScores, topK);

            return new QueryMetrics
            {
                MRR = mrr,
                PrecisionAtK = precisionAtK,
                RecallAtK = recallAtK,
                NDCGAtK = ndcgAtK
            };
        }
    }
}
