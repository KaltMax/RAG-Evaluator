using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents a retrieved chunk result for a query,
    /// including similarity score and relevance labeling for metrics calculation.
    /// </summary>
    public class QueryResult
    {
        public Guid Id { get; init; }
        public Guid QueryId { get; init; }

        // Denormalized chunk data (preserved even if original chunk is deleted -> reproducible and comparable results)
        public Guid DocumentChunkId { get; init; }
        public Guid DocumentId { get; init; }
        public string FileName { get; set; } = string.Empty;
        public string ChunkText { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;

        // Retrieval metadata
        public int Rank { get; set; }
        public double SimilarityScore { get; set; }

        // Relevance labeling (for metrics calculation)
        public bool? IsRelevant { get; set; }
        public RelevanceGrade? RelevanceGrade { get; set; }

        // Navigation property
        public Query Query { get; set; } = null!;
    }
}
