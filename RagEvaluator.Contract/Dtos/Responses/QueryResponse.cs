namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Represents the result of a query, including the question, answer, related sources, and metadata.
    /// </summary>
    public class QueryResponse
    {
        public Guid QueryId { get; set; }
        public required string Question { get; set; }
        public required string Answer { get; set; }
        public List<SearchResultDto> Sources { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
