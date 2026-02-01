namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Represents a document chunk for API responses.
    /// </summary>
    public class DocumentChunkResponse
    {
        public Guid Id { get; set; }
        public required string Text { get; set; }
        public required string ChunkingStrategy { get; set; }
        public required string EmbeddingModel { get; set; }
        public Guid DocumentId { get; set; }
    }
}
