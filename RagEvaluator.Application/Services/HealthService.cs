using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Abstractions.Services;

namespace RagEvaluator.Application.Services
{
    /// <summary>
    /// Reports readiness of the underlying Ollama-backed services (embeddings + chat).
    /// </summary>
    public class HealthService : IHealthService
    {
        private readonly IEmbeddingService _embeddingService;
        private readonly IChatService _chatService;

        public HealthService(IEmbeddingService embeddingService, IChatService chatService)
        {
            _embeddingService = embeddingService;
            _chatService = chatService;
        }

        public async Task<bool> IsReadyAsync(CancellationToken cancellationToken = default)
        {
            return await _embeddingService.IsAvailableAsync(cancellationToken)
                && await _chatService.IsAvailableAsync(cancellationToken);
        }
    }
}
