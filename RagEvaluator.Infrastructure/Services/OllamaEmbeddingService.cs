using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using RagEvaluator.Contract.Abstractions.Services;
using RagEvaluator.Contract.Configurations;

namespace RagEvaluator.Infrastructure.Services
{
    /// <summary>
    /// Embedding service using Ollama via Semantic Kernel
    /// </summary>
    public class OllamaEmbeddingService : IEmbeddingService
    {
        private readonly RagConfiguration _config;
        private IEmbeddingGenerator<string, Embedding<float>>? _embeddingGenerator;
        private bool _isInitialized;

        public OllamaEmbeddingService(RagConfiguration config)
        {
            _config = config;
            _ = InitializeAsync();
        }

        private async Task InitializeAsync()
        {
            try
            {
                var kernelBuilder = Kernel.CreateBuilder()
                    .AddOllamaEmbeddingGenerator(
                        endpoint: new Uri(_config.OllamaEndpoint),
                        modelId: _config.EmbeddingModel
                    );

                var kernel = kernelBuilder.Build();
                _embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
                _isInitialized = true;
            }
            catch
            {
                _isInitialized = false;
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default)
        {
            if (!_isInitialized || _embeddingGenerator == null)
            {
                throw new InvalidOperationException("Embedding service not initialized. Ensure Ollama is running.");
            }

            var embedding = await _embeddingGenerator.GenerateAsync(text, cancellationToken: cancellationToken);
            return embedding.Vector.ToArray();
        }

        public Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_isInitialized && _embeddingGenerator != null);
        }

        public async Task ReinitializeAsync()
        {
            _isInitialized = false;
            _embeddingGenerator = null;
            await InitializeAsync();
        }
    }
}
