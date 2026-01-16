namespace RagEvaluator.Domain.Entities
{
    public class Document
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public int PageCount { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    }

    public enum DocumentStatus
    {
        Pending,
        Processing,
        Completed,
        Failed
    }
}
