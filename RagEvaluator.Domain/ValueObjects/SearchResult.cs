namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Represents a search result from the vector store
    /// </summary>
    public class SearchResult
    {
        /// <summary>
        /// The ID of the matching vector entry
        /// </summary>
        public Guid Id { get; set; }

        /// <summary>
        /// The text content of the matching entry
        /// </summary>
        public string Text { get; set; } = string.Empty;

        /// <summary>
        /// Similarity score (0-1, higher is more similar)
        /// </summary>
        public double Similarity { get; set; }

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
