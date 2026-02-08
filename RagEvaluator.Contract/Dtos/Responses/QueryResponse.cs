namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Represents the full result of a query, including the question, answer, sources, configuration, and evaluation metrics.
    /// </summary>
    public class QueryResponse
    {
        public Guid QueryId { get; set; }
        public required string Question { get; set; }
        public required string Language { get; set; }
        public int TopK { get; set; }
        public required string SystemPrompt { get; set; }
        public required string ChunkingStrategy { get; set; }
        public required string EmbeddingModel { get; set; }
        public required string ChatModel { get; set; }
        public required string Answer { get; set; }
        public List<SearchResultDto> Sources { get; set; } = new();
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public int ResponseTimeMs { get; set; }
        public double? Mrr { get; set; }
        public double? PrecisionAtK { get; set; }
        public double? RecallAtK { get; set; }
        public double? NdcgAtK { get; set; }
        public int? ResponseQuality { get; set; }
        public bool? HasLanguageSwitching { get; set; }
    }
}
