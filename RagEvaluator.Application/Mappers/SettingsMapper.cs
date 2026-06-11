using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Mappers
{
    /// <summary>
    /// Extension methods for mapping the runtime RagConfiguration to settings DTOs.
    /// </summary>
    public static class SettingsMapper
    {
        private static readonly List<ChunkingStrategy> AvailableChunkingStrategies =
            [ChunkingStrategy.FixedSize, ChunkingStrategy.Semantic];

        public static SettingsResponse ToResponse(this RagConfiguration config)
        {
            return new SettingsResponse
            {
                EmbeddingModel = config.EmbeddingModel,
                ChunkingStrategy = config.ChunkingStrategy,
                PromptTemplate = config.PromptTemplate,
                ChunkSize = config.ChunkSize,
                ChunkOverlap = config.ChunkOverlap,
                SimilarityThreshold = config.SimilarityThreshold,
                MinChunkSize = config.MinChunkSize,
                PromptBasicText = config.PromptBasic,
                PromptInstructedText = config.PromptInstructed,
                PromptLanguageAwareEnText = config.PromptLanguageAwareEn,
                PromptLanguageAwareDeText = config.PromptLanguageAwareDe,
                AvailableEmbeddingModels = SplitCsv(config.AvailableEmbeddingModels),
                AvailableChunkingStrategies = AvailableChunkingStrategies,
                AvailableCourses = SplitCsv(config.AvailableCourses),
            };
        }

        private static List<string> SplitCsv(string value)
        {
            return value
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }
}
