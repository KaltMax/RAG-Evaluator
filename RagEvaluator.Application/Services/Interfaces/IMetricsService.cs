namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for calculating retrieval and similarity metrics
    /// </summary>
    public interface IMetricsService
    {
        /// <summary>
        /// Calculates cosine similarity between two vectors (1 = identical, 0 = orthogonal, -1 = opposite)
        /// </summary>
        double CosineSimilarity(float[] a, float[] b);

        /// <summary>
        /// Calculates cosine distance between two vectors (0 = identical, 1 = orthogonal, 2 = opposite)
        /// </summary>
        double CosineDistance(float[] a, float[] b);

        /// <summary>
        /// Calculates Mean Reciprocal Rank (MRR) for a set of queries
        /// </summary>
        double MeanReciprocalRank(IReadOnlyList<int?> relevantRanks);

        /// <summary>
        /// Calculates Precision@K - the proportion of retrieved documents that are relevant
        /// </summary>
        double PrecisionAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);

        /// <summary>
        /// Calculates Recall@K - the proportion of relevant documents that were retrieved
        /// </summary>
        double RecallAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);

        /// <summary>
        /// Calculates Normalized Discounted Cumulative Gain (NDCG@K)
        /// </summary>
        double NormalizedDiscountedCumulativeGainAtK(IReadOnlyList<double> relevanceScores, int k);
    }
}
