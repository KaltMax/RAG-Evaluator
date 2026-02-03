using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Lightweight document representation for list views, excluding content and file path.
    /// </summary>
    public class DocumentSummary
    {
        public Guid Id { get; set; }
        public string FileName { get; set; } = string.Empty;
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public string? Language { get; set; }
        public int PageCount { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public DocumentStatus Status { get; set; }
    }
}
