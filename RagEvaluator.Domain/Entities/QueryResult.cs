namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents a retrieved chunk result for a query,
    /// including similarity score and relevance labeling for metrics calculation.
    /// </summary>
    public class QueryResult
    {
        public Guid Id { get; set; }
        public Guid QueryId { get; set; }

        // Denormalized chunk data (preserved even if original chunk is deleted -> reproducible and comparable results)
        public Guid DocumentChunkId { get; set; }
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ChunkText { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;

        // Retrieval metadata
        public int Rank { get; set; }
        public double SimilarityScore { get; set; }

        // Relevance labeling (for metrics calculation)
        public bool? IsRelevant { get; set; }
        public int? RelevanceGrade { get; set; }  // 0-3 scale for NDCG

        // Navigation property
        public Query Query { get; set; } = null!;
    }
}
