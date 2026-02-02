namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents a user query for document retrieval and processing,
    /// including response data and evaluation metrics.
    /// </summary>
    public class Query
    {
        // Query parameters
        public Guid Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int TopK { get; set; } = 3;
        public string SystemPrompt { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public string ChatModel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Response data
        public string Answer { get; set; } = string.Empty;
        public float[] QueryEmbedding { get; set; } = [];
        public int ResponseTimeMs { get; set; }

        // RAG Metrics (nullable - calculated after relevance labeling)
        public double? MRR { get; set; }
        public double? PrecisionAtK { get; set; }
        public double? RecallAtK { get; set; }
        public double? NDCGAtK { get; set; }

        // Navigation property for related QueryResults
        public ICollection<QueryResult> Results { get; set; } = [];
    }
}
