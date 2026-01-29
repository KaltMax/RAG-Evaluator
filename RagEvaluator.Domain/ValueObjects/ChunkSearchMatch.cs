namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Represents a raw chunk match from vector search (before similarity calculation)
    /// </summary>
    public class ChunkSearchMatch
    {
        /// <summary>
        /// The ID of the matching chunk
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The text content of the matching chunk
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// The embedding vector of the chunk
        /// </summary>
        public float[] Embedding { get; set; } = [];

        /// <summary>
        /// The ID of the document this chunk belongs to
        /// </summary>
        public Guid DocumentId { get; set; }

        /// <summary>
        /// The file name of the source document
        /// </summary>
        public string FileName { get; set; } = string.Empty;

        /// <summary>
        /// The chunking strategy used to create this chunk
        /// </summary>
        public string ChunkingStrategy { get; set; } = string.Empty;

        /// <summary>
        /// The embedding model used to generate this vector
        /// </summary>
        public string EmbeddingModel { get; set; } = string.Empty;
    }
}
