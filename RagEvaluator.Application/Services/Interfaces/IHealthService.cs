namespace RagEvaluator.Application.Services.Interfaces
{
    /// <summary>
    /// Reports whether the RAG backend services (embeddings + chat) are ready to handle requests.
    /// </summary>
    public interface IHealthService
    {
        Task<bool> IsReadyAsync(CancellationToken cancellationToken = default);
    }
}
