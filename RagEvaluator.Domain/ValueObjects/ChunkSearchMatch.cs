namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Represents a raw chunk match from vector search (before similarity calculation)
    /// </summary>
    public class ChunkSearchMatch
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = [];
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
    }
}
