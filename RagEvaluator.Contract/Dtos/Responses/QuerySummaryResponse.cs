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
        public required string ChunkingStrategy { get; set; }
        public required string EmbeddingModel { get; set; }
        public required string ChatModel { get; set; }
        public required string Answer { get; set; }
        public DateTime CreatedAt { get; set; }
        public int ResponseTimeMs { get; set; }
        public double? Mrr { get; set; }
        public double? PrecisionAtK { get; set; }
        public double? RecallAtK { get; set; }
        public double? NdcgAtK { get; set; }
        public int? ResponseQuality { get; set; }
        public bool? HasLanguageSwitching { get; set; }
        public Guid? ExperimentId { get; set; }
        public string? ExperimentName { get; set; }
    }
}
