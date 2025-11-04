namespace RagEvaluator.Application.Services.Interfaces
{
    public interface ITextChunker
    {
        List<string> SplitDocuments(List<string> documents);
        List<string> SplitText(string text);
    }
}
