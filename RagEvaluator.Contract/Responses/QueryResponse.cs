namespace RagEvaluator.Contract.Responses
{
    public class QueryResponse
    {
        public Guid QueryId { get; set; }
        public required string Question { get; set; }
        public required string Answer { get; set; }
        public List<SearchResultDto> Sources { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
