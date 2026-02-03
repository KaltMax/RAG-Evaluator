namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Represents a search result from the vector store
    /// </summary>
    public class SearchResult
    {
        public Guid Id { get; init; }
        public string Text { get; init; } = string.Empty;
        public double Similarity { get; init; }
        public Guid DocumentId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string ChunkingStrategy { get; init; } = string.Empty;
        public string EmbeddingModel { get; init; } = string.Empty;
    }
}
