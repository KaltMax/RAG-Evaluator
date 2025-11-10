namespace RagEvaluator.Contract.Abstractions.Services
{
    /// <summary>
    /// Defines methods for splitting text or documents into smaller chunks for processing or analysis.
    /// </summary>
    public interface ITextChunker
    {
        List<string> SplitDocuments(List<string> documents);
        List<string> SplitText(string text);
    }
}
