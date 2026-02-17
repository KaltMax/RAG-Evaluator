using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Represents a document uploaded by a user.
    /// </summary>
    public class Document
    {
        public Guid Id { get; init; }
        public string FileName { get; set; } = string.Empty;
        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public string? Content { get; set; }
        public string? Language { get; set; }
        public string? Course { get; set; }
        public int PageCount { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; init; }
        public DateTime? ProcessedAt { get; set; }
        public DocumentStatus Status { get; set; } = DocumentStatus.Pending;
    }
}
