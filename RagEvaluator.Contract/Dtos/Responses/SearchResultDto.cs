namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Represents the result of a search operation, including the matched item's identifier, text, similarity score,
    /// and document metadata.
    /// </summary>
    public class SearchResultDto
    {
        public Guid Id { get; set; }
        public required string Text { get; set; }
        public double Similarity { get; set; }
        public Guid DocumentId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string ChunkingStrategy { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
    }
}
