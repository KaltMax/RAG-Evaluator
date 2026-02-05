namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents a chunk of a document with its text and vector embedding
    /// </summary>
    public class DocumentChunk
    {
        public Guid Id { get; init; }
        public string Text { get; set; } = string.Empty;
        public float[] Embedding { get; set; } = Array.Empty<float>();
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public Guid DocumentId { get; init; }
    }
}
