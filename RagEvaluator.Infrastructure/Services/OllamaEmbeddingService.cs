using Microsoft.Extensions.AI;
using Microsoft.SemanticKernel;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Models;

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
                var kernelBuilder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0010
                kernelBuilder.AddOpenAIEmbeddingGenerator(
                    modelId: _config.EmbeddingModel,
                    apiKey: "ollama",
                    httpClient: new System.Net.Http.HttpClient { BaseAddress = new Uri(_config.OllamaEndpoint) }
                );
#pragma warning restore SKEXP0010

                var kernel = kernelBuilder.Build();
                _embeddingGenerator = kernel.GetRequiredService<IEmbeddingGenerator<string, Embedding<float>>>();
                _isInitialized = true;
            }
            catch
            {
                _isInitialized = false;
            }
        }

        public async Task<float[]> GenerateEmbeddingAsync(string text)
        {
            if (!_isInitialized || _embeddingGenerator == null)
            {
                throw new InvalidOperationException("Embedding service not initialized. Ensure Ollama is running.");
            }

            var embedding = await _embeddingGenerator.GenerateAsync(text);
            return embedding.Vector.ToArray();
        }

        public Task<bool> IsAvailableAsync()
        {
            return Task.FromResult(_isInitialized && _embeddingGenerator != null);
        }
    }
}
