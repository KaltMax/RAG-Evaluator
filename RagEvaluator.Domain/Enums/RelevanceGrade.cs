namespace RagEvaluator.Domain.Enums
{
    /// <summary>
    /// Graded relevance scale for IR evaluation metrics (NDCG).
    /// </summary>
    public enum RelevanceGrade
    {
        NotRelevant = 0,
        Related = 1,
        HighlyRelevant = 2
    }
}
