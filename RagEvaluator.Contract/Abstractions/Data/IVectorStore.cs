using RagEvaluator.Domain.ValueObjects;

namespace RagEvaluator.Contract.Abstractions.Data
{
    /// <summary>
    /// Defines the contract for a vector store that supports adding entries, searching for similar vectors, and
    /// clearing all stored data.
    /// </summary>
    public interface IVectorStore
    {
        int Count { get; }
        void AddEntry(int id, string text, float[] embedding, Dictionary<string, object>? metadata = null);
        List<SearchResult> Search(float[] queryEmbedding, int topK = 3);
        void Clear();
    }
}
