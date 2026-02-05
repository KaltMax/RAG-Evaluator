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
        /// Calculates Mean Reciprocal Rank (MRR): evaluates how high the first relevant result is ranked.
        /// MRR = 1/rank of the first relevant result. Higher is better (max 1.0).
        /// </summary>
        double MeanReciprocalRank(IReadOnlyList<int?> relevantRanks);
        
        /// <summary>
        /// Calculates Precision@K: the proportion of relevant document chunks in the top K retrieved results.
        /// Precision@K = (number of relevant chunks in top K) / K.
        /// </summary>
        double PrecisionAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);
        
        /// <summary>
        /// Calculates Recall@K at document level: the proportion of relevant documents found in the top K results.
        /// Recall@K = (number of relevant documents found in top K) / (total number of relevant documents).
        /// </summary>
        double RecallAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);
        
        /// <summary>
        /// Calculates Normalized Discounted Cumulative Gain (NDCG@K): measures ranking quality.
        /// Considers both relevance grades and position — higher-ranked results contribute more to the score.
        /// Uses graded relevance (0-3) rather than binary relevance.
        /// </summary>
        double NormalizedDiscountedCumulativeGainAtK(IReadOnlyList<double> relevanceScores, int k);
        
        /// <summary>
        /// Calculates all retrieval metrics (MRR, Precision@K, Recall@K, NDCG@K) for a set of query results.
        /// </summary>
        /// <param name="results">The query results to evaluate.</param>
        /// <param name="topK">The number of top results to consider.</param>
        /// <param name="groundTruthDocumentIds">Ground truth relevant document IDs for Recall@K calculation.</param>
        QueryMetrics CalculateQueryMetrics(IReadOnlyList<QueryResult> results, int topK, IReadOnlyList<Guid> groundTruthDocumentIds);
    }
}
