namespace RagEvaluator.Domain.ValueObjects
{
    /// <summary>
    /// Value object representing document metadata
    /// </summary>
    public class DocumentMetadata
    {
        public Guid DocumentId { get; init; }
        public string FileName { get; init; }
        public string? Description { get; init; }
        public int PageCount { get; init; }
        public int ChunkCount { get; init; }
        public DateTime UploadedAt { get; init; }

        public DocumentMetadata(
            Guid documentId,
            string fileName,
            string? description,
            int pageCount,
            int chunkCount,
            DateTime uploadedAt)
        {
            DocumentId = documentId;
            FileName = fileName ?? throw new ArgumentNullException(nameof(fileName));
            Description = description;
            PageCount = pageCount;
            ChunkCount = chunkCount;
            UploadedAt = uploadedAt;
        }
    }
}
