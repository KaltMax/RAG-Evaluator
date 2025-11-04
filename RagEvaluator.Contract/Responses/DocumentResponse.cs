namespace RagEvaluator.Contract.Responses
{
    public class DocumentResponse
    {
        public Guid DocumentId { get; set; }
        public required string FileName { get; set; }
        public string? Description { get; set; }
        public int PageCount { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
