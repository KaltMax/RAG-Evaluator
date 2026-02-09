namespace RagEvaluator.Contract.Dtos.Responses
{
    public class ExperimentSummaryResponse
    {
        public Guid Id { get; set; }
        public required string Name { get; set; }
        public required string Status { get; set; }
        public int RepeatCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public required ExperimentProgress Progress { get; set; }

        // Configuration snapshot
        public required string EmbeddingModel { get; set; }
        public required string ChunkingStrategy { get; set; }
        public required string PromptTemplate { get; set; }
    }
}
