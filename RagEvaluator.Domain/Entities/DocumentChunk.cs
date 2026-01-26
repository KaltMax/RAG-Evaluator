namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents a chunk of a document with its text and vector embedding
    /// </summary>
    public class DocumentChunk
    {
        /// <summary>
        /// Unique identifier for the entry
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// Gets or sets the text content associated with this instance.
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The vector embedding representation of the text
        /// </summary>
        public float[] Embedding { get; set; } = Array.Empty<float>();

        /// <summary>
        /// The chunking strategy used to create this text chunk
        /// </summary>
        public string ChunkingStrategy { get; set; } = string.Empty;

        /// <summary>
        /// The embedding model used to generate this vector
        /// </summary>
        public string EmbeddingModel { get; set; } = string.Empty;

        /// <summary>
        /// The ID of the document this vector entry is associated with
        /// </summary>
        public Guid DocumentId { get; set; }
    }
}
