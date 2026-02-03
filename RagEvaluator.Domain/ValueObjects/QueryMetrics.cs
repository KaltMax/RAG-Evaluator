namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Represents RAG metrics related to a query.
    /// </summary>
    public class QueryMetrics
    {
        public double MRR { get; init; }
        public double PrecisionAtK { get; init; }
        public double RecallAtK { get; init; }
        public double NDCGAtK { get; init; }
    }
}
