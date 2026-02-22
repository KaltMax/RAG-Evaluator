namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines a contract for generating vector embeddings from text and checking service availability.
    /// </summary>
    public interface IEmbeddingService
    {
        /// <summary>
        /// Generates a vector embedding for a search query, applying model-specific query prefixes.
        /// </summary>
        Task<float[]> GenerateQueryEmbeddingAsync(string text, CancellationToken cancellationToken = default);

        /// <summary>
        /// Generates a vector embedding for a document chunk, applying model-specific document prefixes.
        /// </summary>
        Task<float[]> GenerateDocumentEmbeddingAsync(string text, CancellationToken cancellationToken = default);

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
