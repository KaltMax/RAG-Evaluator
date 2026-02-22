using System.ComponentModel.DataAnnotations;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Partial update request for runtime RAG configuration settings.
    /// Only provided (non-null) fields are applied.
    /// </summary>
    public class UpdateSettingsRequest
    {
        public string? EmbeddingModel { get; set; }
        public ChunkingStrategy? ChunkingStrategy { get; set; }
        public PromptTemplate? PromptTemplate { get; set; }

        [Range(1, 1500)]
        public int? ChunkSize { get; set; }

        [Range(0, 1000)]
        public int? ChunkOverlap { get; set; }

        [Range(0.0, 1.0)]
        public double? SimilarityThreshold { get; set; }
    }
}
