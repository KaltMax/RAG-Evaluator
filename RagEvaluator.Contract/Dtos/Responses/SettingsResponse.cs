using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Current runtime RAG configuration with available options for UI dropdowns.
    /// </summary>
    public class SettingsResponse
    {
        // Active settings
        public required string EmbeddingModel { get; set; }
        public required ChunkingStrategy ChunkingStrategy { get; set; }
        public required PromptTemplate PromptTemplate { get; set; }
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public double SimilarityThreshold { get; set; }
        public int TopK { get; set; }

        // Available options
        public required List<string> AvailableEmbeddingModels { get; set; }
        public required List<ChunkingStrategy> AvailableChunkingStrategies { get; set; }
        public required List<PromptTemplate> AvailablePromptTemplates { get; set; }
    }
}
