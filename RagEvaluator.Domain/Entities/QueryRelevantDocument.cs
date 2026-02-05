namespace RagEvaluator.Domain.Entities;

/// <summary>
/// Represents a ground truth relevant document for a query, used for Recall@K calculation.
/// </summary>
public class QueryRelevantDocument
{
    public Guid QueryId { get; init; }
    public Guid DocumentId { get; init; }
    public Query Query { get; set; } = null!;
}
