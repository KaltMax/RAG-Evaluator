using RagEvaluator.Application.Mappers;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Manages runtime RAG configuration: reading, validating, and applying settings changes.
    /// </summary>
    public class SettingsService : ISettingsService
    {
        private readonly RagConfiguration _config;
        private readonly IEmbeddingService _embeddingService;

        public SettingsService(RagConfiguration config, IEmbeddingService embeddingService)
        {
            _config = config;
            _embeddingService = embeddingService;
        }

        public SettingsResponse GetSettings() => _config.ToResponse();

        public async Task<SettingsResponse> UpdateSettingsAsync(UpdateSettingsRequest request)
        {
            ValidateRequest(request);

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

            return _config.ToResponse();
        }

        private void ValidateRequest(UpdateSettingsRequest request)
        {
            var availableModels = ParseAvailableModels();
            if (request.EmbeddingModel is not null && !availableModels.Contains(request.EmbeddingModel))
            {
                throw new ArgumentException($"Unknown embedding model '{request.EmbeddingModel}'. Available: {string.Join(", ", availableModels)}");
            }

            // Use effective values so partial updates are validated against the resulting configuration.
            var effectiveChunkSize = request.ChunkSize ?? _config.ChunkSize;
            var effectiveOverlap = request.ChunkOverlap ?? _config.ChunkOverlap;
            if (effectiveOverlap >= effectiveChunkSize)
            {
                throw new ArgumentException("Chunk overlap must be less than chunk size.");
            }
        }

        private List<string> ParseAvailableModels()
        {
            return _config.AvailableEmbeddingModels
                .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .ToList();
        }
    }
}
