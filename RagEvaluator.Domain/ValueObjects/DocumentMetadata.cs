namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing document metadata
    /// </summary>
    public class DocumentMetadata
    {
        public Guid DocumentId { get; init; }
        public required string FileName { get; init; }
        public string? Description { get; init; }
        public int PageCount { get; init; }
        public int ChunkCount { get; init; }
        public DateTime UploadedAt { get; init; }
    }
}
