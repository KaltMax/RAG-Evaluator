using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Manages runtime RAG configuration: reading, validating, and applying settings changes.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly RagConfiguration _config;
        private readonly IEmbeddingService _embeddingService;

        private static readonly List<ChunkingStrategy> AvailableChunkingStrategies =
            [ChunkingStrategy.FixedSize, ChunkingStrategy.Semantic];

        public SettingsService(RagConfiguration config, IEmbeddingService embeddingService)
        {
            _config = config;
            _embeddingService = embeddingService;
        }

        public SettingsResponse GetSettings() => BuildResponse();

        public async Task<SettingsResponse> UpdateSettingsAsync(UpdateSettingsRequest request)
        {
            // Validate embedding model against available models
            var availableModels = ParseAvailableModels();
            if (request.EmbeddingModel is not null && !availableModels.Contains(request.EmbeddingModel))
            {
                throw new ArgumentException($"Unknown embedding model '{request.EmbeddingModel}'. Available: {string.Join(", ", availableModels)}");
            }
                
            // Validate chunk overlap against chunk size (using effective values for partial updates)
            var effectiveChunkSize = request.ChunkSize ?? _config.ChunkSize;
            var effectiveOverlap = request.ChunkOverlap ?? _config.ChunkOverlap;
            if (effectiveOverlap >= effectiveChunkSize)
            {
                throw new ArgumentException("Chunk overlap must be less than chunk size.");
            }

            var embeddingModelChanged = request.EmbeddingModel is not null && request.EmbeddingModel != _config.EmbeddingModel;

            if (request.EmbeddingModel is not null)
            {
                _config.EmbeddingModel = request.EmbeddingModel;
            }
            if (request.ChunkingStrategy is not null)
            {
                _config.ChunkingStrategy = request.ChunkingStrategy.Value;
            }
            if (request.PromptTemplate is not null)
            {
                _config.PromptTemplate = request.PromptTemplate.Value;
            }
            if (request.ChunkSize is not null)
            {
                _config.ChunkSize = request.ChunkSize.Value;
            }
            if (request.ChunkOverlap is not null)
            {
                _config.ChunkOverlap = request.ChunkOverlap.Value;
            }
            if (request.SimilarityThreshold is not null)
            {
                _config.SimilarityThreshold = request.SimilarityThreshold.Value;
            }
            if (request.MinChunkSize is not null)
            {
                _config.MinChunkSize = request.MinChunkSize.Value;
            }
            if (embeddingModelChanged)
            { 
                await _embeddingService.ReinitializeAsync(); 
            }

            return BuildResponse();
        }

        private SettingsResponse BuildResponse()
        {
            return new SettingsResponse
            {
                EmbeddingModel = _config.EmbeddingModel,
                ChunkingStrategy = _config.ChunkingStrategy,
                PromptTemplate = _config.PromptTemplate,
                ChunkSize = _config.ChunkSize,
                ChunkOverlap = _config.ChunkOverlap,
                SimilarityThreshold = _config.SimilarityThreshold,
                MinChunkSize = _config.MinChunkSize,
                PromptBasicText = _config.PromptBasic,
                PromptInstructedText = _config.PromptInstructed,
                PromptLanguageAwareEnText = _config.PromptLanguageAwareEn,
                PromptLanguageAwareDeText = _config.PromptLanguageAwareDe,
                AvailableEmbeddingModels = ParseAvailableModels(),
                AvailableChunkingStrategies = AvailableChunkingStrategies,
                AvailableCourses = ParseAvailableCourses(),
            };
        }

        private List<string> ParseAvailableModels()
        {
            return _config.AvailableEmbeddingModels
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }

        private List<string> ParseAvailableCourses()
        {
            return _config.AvailableCourses
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }
}
