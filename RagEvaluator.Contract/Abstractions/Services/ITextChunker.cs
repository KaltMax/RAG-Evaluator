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
        List<string> SplitDocuments(List<string> documents);
        
        /// <summary>
        /// Splits a single text into smaller chunks.
        /// </summary>
        List<string> SplitText(string text);
    }
}
