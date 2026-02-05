namespace RagEvaluator.Domain.Entities;

/// <summary>
/// Represents a ground truth relevant document for a query, used for Recall@K calculation.
/// </summary>
public class QueryRelevantDocument
{
    public Guid QueryId { get; set; }
    public Guid DocumentId { get; set; }
    public Query Query { get; set; } = null!;
}
