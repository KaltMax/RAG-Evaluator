namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for splitting text or documents into smaller chunks for processing or analysis.
    /// </summary>
    public interface ITextChunker
    {
        /// <summary>
        /// Splits a single document into smaller chunks.
        /// </summary>
        Task<List<string>> CreateDocumentChunksAsync(string documentContent, CancellationToken cancellationToken = default);
    }
}
