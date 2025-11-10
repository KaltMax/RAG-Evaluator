namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines a contract for generating vector embeddings from text and checking service availability.
    /// </summary>
    public interface IEmbeddingService
    {
        Task<float[]> GenerateEmbeddingAsync(string text);
        Task<bool> IsAvailableAsync();
    }
}
