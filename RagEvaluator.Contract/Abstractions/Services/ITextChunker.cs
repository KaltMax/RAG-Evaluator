namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for splitting text or documents into smaller chunks for processing or analysis.
    /// </summary>
    public interface ITextChunker
    {
        /// <summary>
        /// Splits a list of documents into smaller text chunks.
        /// </summary>
        Task<List<string>> SplitDocumentsAsync(List<string> documents, CancellationToken cancellationToken = default);

        /// <summary>
        /// Splits a single text into smaller chunks.
        /// </summary>
        Task<List<string>> SplitTextAsync(string text, CancellationToken cancellationToken = default);
    }
}
