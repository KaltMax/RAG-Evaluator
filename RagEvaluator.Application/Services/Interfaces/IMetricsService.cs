using RagEvaluator.Domain.Entities;
using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for calculating retrieval and similarity metrics
    /// </summary>
    public interface IMetricsService
    {
        /// <summary>
        /// Calculates the cosine similarity between two vectors (ranges from 0 to 1, where 1 means identical).
        /// </summary>
        double CosineSimilarity(float[] a, float[] b);
        
        /// <summary>
        /// Calculates the cosine distance between two vectors (1 - cosine similarity, ranges from 0 to 1).
        /// </summary>
        double CosineDistance(float[] a, float[] b);
        
        /// <summary>
        /// Calculates the Mean Reciprocal Rank (MRR) from a list of ranks where relevant results were found.
        /// </summary>
        double MeanReciprocalRank(IReadOnlyList<int?> relevantRanks);
        
        /// <summary>
        /// Calculates Precision@K: the proportion of relevant documents in the top K retrieved results.
        /// </summary>
        double PrecisionAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);
        
        /// <summary>
        /// Calculates Recall@K: the proportion of all relevant documents that appear in the top K results.
        /// </summary>
        double RecallAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);
        
        /// <summary>
        /// Calculates Normalized Discounted Cumulative Gain (NDCG@K): measures ranking quality considering relevance scores and position.
        /// </summary>
        double NormalizedDiscountedCumulativeGainAtK(IReadOnlyList<double> relevanceScores, int k);
        
        /// <summary>
        /// Calculates all retrieval metrics (MRR, Precision@K, Recall@K, NDCG@K) for a set of query results.
        /// </summary>
        QueryMetrics CalculateQueryMetrics(IReadOnlyList<QueryResult> results, int topK);
    }
}
