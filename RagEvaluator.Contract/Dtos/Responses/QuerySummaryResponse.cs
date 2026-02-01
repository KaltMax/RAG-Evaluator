namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Summary of a persisted query for history/listing purposes.
    /// </summary>
    public class QuerySummaryResponse
    {
        public Guid Id { get; set; }
        public required string Question { get; set; }
        public required string Language { get; set; }
        public int TopK { get; set; }
        public required string SystemPrompt { get; set; }
        public required string EmbeddingModel { get; set; }
        public required string ChatModel { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
