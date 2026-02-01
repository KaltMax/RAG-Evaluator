namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents a user query for document retrieval and processing.
    /// </summary>
    public class Query
    {
        public Guid Id { get; set; }
        public string Question { get; set; } = string.Empty;
        public string Language { get; set; } = string.Empty;
        public int TopK { get; set; } = 3;
        public string SystemPrompt { get; set; } = string.Empty;
        public string EmbeddingModel { get; set; } = string.Empty;
        public string ChatModel { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
        }
}
