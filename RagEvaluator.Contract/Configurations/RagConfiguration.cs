using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Contract.Configurations
{
    /// <summary>
    /// Represents the configuration settings for a Retrieval-Augmented Generation (RAG) workflow, including model
    /// endpoints, chunking parameters, and system prompt customization.
    /// </summary>
    public class RagConfiguration
    {
        public string OllamaEndpoint { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public string AvailableEmbeddingModels { get; set; } = string.Empty;
        public string ChatModel { get; set; } = string.Empty;
        public ChunkingStrategy ChunkingStrategy { get; set; }
        public PromptTemplate PromptTemplate { get; set; }
        public string PromptBasic { get; set; } = string.Empty;
        public string PromptInstructed { get; set; } = string.Empty;
        public string PromptLanguageAwareEn { get; set; } = string.Empty;
        public string PromptLanguageAwareDe { get; set; } = string.Empty;
        public int ChunkSize { get; set; }
        public int ChunkOverlap { get; set; }
        public double SimilarityThreshold { get; set; }
        public int MinChunkSize { get; set; }
        public string AvailableCourses { get; set; } = string.Empty;

        /// <summary>
        /// Validates that required values are present and within range. Called once at startup so a
        /// misconfiguration fails fast with a clear message instead of surfacing later at query time.
        /// </summary>
        public void Validate()
        {
            var errors = new List<string>();

            // Required, environment-specific values — no safe default exists.
            if (string.IsNullOrWhiteSpace(OllamaEndpoint))
                errors.Add($"{nameof(OllamaEndpoint)} is required.");
            if (string.IsNullOrWhiteSpace(AvailableEmbeddingModels))
                errors.Add($"{nameof(AvailableEmbeddingModels)} is required.");
            if (string.IsNullOrWhiteSpace(EmbeddingModel))
                errors.Add($"{nameof(EmbeddingModel)} could not be resolved from {nameof(AvailableEmbeddingModels)}.");
            if (string.IsNullOrWhiteSpace(ChatModel))
                errors.Add($"{nameof(ChatModel)} is required.");

            // Required prompt texts.
            if (string.IsNullOrWhiteSpace(PromptBasic))
                errors.Add($"{nameof(PromptBasic)} is required.");
            if (string.IsNullOrWhiteSpace(PromptInstructed))
                errors.Add($"{nameof(PromptInstructed)} is required.");
            if (string.IsNullOrWhiteSpace(PromptLanguageAwareEn))
                errors.Add($"{nameof(PromptLanguageAwareEn)} is required.");
            if (string.IsNullOrWhiteSpace(PromptLanguageAwareDe))
                errors.Add($"{nameof(PromptLanguageAwareDe)} is required.");

            // Numeric ranges.
            if (ChunkSize <= 0)
                errors.Add($"{nameof(ChunkSize)} must be greater than 0.");
            if (ChunkOverlap < 0)
                errors.Add($"{nameof(ChunkOverlap)} must be 0 or greater.");
            if (ChunkOverlap >= ChunkSize)
                errors.Add($"{nameof(ChunkOverlap)} must be less than {nameof(ChunkSize)}.");
            if (MinChunkSize < 0)
                errors.Add($"{nameof(MinChunkSize)} must be 0 or greater.");
            if (SimilarityThreshold is < 0.0 or > 1.0)
                errors.Add($"{nameof(SimilarityThreshold)} must be between 0.0 and 1.0.");

            if (errors.Count > 0)
            {
                throw new InvalidOperationException(
                    "Invalid RagConfiguration:" + Environment.NewLine +
                    string.Join(Environment.NewLine, errors.Select(e => "  - " + e)));
            }
        }
    }
}
