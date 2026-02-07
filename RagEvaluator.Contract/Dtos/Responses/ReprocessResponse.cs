namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Response returned after reprocessing all documents with the current configuration.
    /// </summary>
    public class ReprocessResponse
    {
        public int DocumentsProcessed { get; set; }
        public int TotalChunksCreated { get; set; }
        public required string ChunkingStrategy { get; set; }
        public required string EmbeddingModel { get; set; }
    }
}
