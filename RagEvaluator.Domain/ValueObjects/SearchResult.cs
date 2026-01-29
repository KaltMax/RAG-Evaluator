namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Represents a search result from the vector store
    /// </summary>
    public class SearchResult
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public double Similarity { get; set; }
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
    }
}
