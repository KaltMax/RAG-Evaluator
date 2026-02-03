namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Represents a raw chunk match from vector search (before similarity calculation)
    /// </summary>
    public class ChunkSearchMatch
    {
        public Guid Id { get; init; }
        public string Text { get; init; } = string.Empty;
        public float[] Embedding { get; init; } = [];
        public Guid DocumentId { get; init; }
        public string FileName { get; init; } = string.Empty;
        public string ChunkingStrategy { get; init; } = string.Empty;
        public string EmbeddingModel { get; init; } = string.Empty;
    }
}
