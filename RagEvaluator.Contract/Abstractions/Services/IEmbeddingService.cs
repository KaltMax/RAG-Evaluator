namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines a contract for generating vector embeddings from text and checking service availability.
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates a vector embedding from the provided text.
        /// </summary>
        Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Checks whether the embedding service is available and ready to generate embeddings.
        /// </summary>
        Task<bool> IsAvailableAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Reinitializes the embedding service with the current configuration (e.g. after changing the model).
        /// </summary>
        Task ReinitializeAsync();
    }
}
