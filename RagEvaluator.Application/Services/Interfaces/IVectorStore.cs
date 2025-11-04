using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Application.Services.Interfaces
{
    public interface IVectorStore
    {
        int Count { get; }
        void AddEntry(int id, string text, float[] embedding, Dictionary<string, object>? metadata = null);
        List<SearchResult> Search(float[] queryEmbedding, int topK = 3);
        void Clear();
    }
}
