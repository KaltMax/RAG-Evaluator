namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Represents the result of a search operation, including the matched item's identifier, text, similarity score,
    /// and optional metadata.
    /// </summary>
    public class SearchResultDto
    {
        public int Id { get; set; }
        public required string Text { get; set; }
        public float Similarity { get; set; }
        public Dictionary<string, object>? Metadata { get; set; }
    }
}
