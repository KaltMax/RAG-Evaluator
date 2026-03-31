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
        public int MinChunkSize { get; set; }

        // Available options
        public required List<string> AvailableEmbeddingModels { get; set; }
        public required List<ChunkingStrategy> AvailableChunkingStrategies { get; set; }
        public required List<string> AvailableCourses { get; set; }

        // Prompt template texts
        public required string PromptBasicText { get; set; }
        public required string PromptInstructedText { get; set; }
        public required string PromptLanguageAwareEnText { get; set; }
        public required string PromptLanguageAwareDeText { get; set; }
    }
}
