namespace RagEvaluator.Domain.Enums
{
    /// <summary>
    /// Graded relevance scale for IR evaluation metrics (NDCG).
    /// </summary>
    public enum RelevanceGrade
    {
        NotRelevant = 0,
        MarginallyRelevant = 1,
        FairlyRelevant = 2,
        HighlyRelevant = 3
    }
}
