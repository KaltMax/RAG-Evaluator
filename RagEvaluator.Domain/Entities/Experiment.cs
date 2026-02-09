using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents an experiment that runs a set of test queries multiple times against a specific RAG configuration
    /// to gather statistically reliable evaluation data.
    /// </summary>
    public class Experiment
    {
        public Guid Id { get; init; }
        public string Name { get; set; } = string.Empty;
        public int RepeatCount { get; set; }
        public ExperimentStatus Status { get; set; } = ExperimentStatus.Running;
        public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
        public DateTime? CompletedAt { get; set; }

        // Config snapshot (captured at creation from current settings)
        public string EmbeddingModel { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string ChatModel { get; set; } = string.Empty;
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public double SimilarityThreshold { get; set; }
        public string PromptTemplate { get; set; } = string.Empty;

        // Progress tracking
        public int TotalQueryCount { get; set; }
        public int CompletedQueryCount { get; set; }

        // Navigation
        public ICollection<Query> Queries { get; set; } = [];
    }
}
