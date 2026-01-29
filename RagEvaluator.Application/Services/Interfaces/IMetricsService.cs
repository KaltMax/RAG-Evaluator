namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Service for calculating retrieval and similarity metrics
    /// </summary>
    public interface IMetricsService
    {
        double CosineSimilarity(float[] a, float[] b);
        double CosineDistance(float[] a, float[] b);
        double MeanReciprocalRank(IReadOnlyList<int?> relevantRanks);
        double PrecisionAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);
        double RecallAtK(IReadOnlyList<string> retrievedIds, IReadOnlyList<string> relevantIds, int k);
        double NormalizedDiscountedCumulativeGainAtK(IReadOnlyList<double> relevanceScores, int k);
    }
}
