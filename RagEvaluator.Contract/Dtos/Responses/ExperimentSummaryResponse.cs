namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Represents a summary of an experiment, including its ID, name, status, progress, and configuration snapshot.
    /// </summary>
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
        public required string ChatModel { get; set; }
        public required string EmbeddingModel { get; set; }
        public required string ChunkingStrategy { get; set; }
        public required string PromptTemplate { get; set; }
    }
}
