using RagEvaluator.Domain.Enums;

namespace RagEvaluator.Domain.Entities
{
    /// <summary>
    /// Lightweight document representation for list views, excluding content and file path.
    /// </summary>
    public class DocumentSummary
    {
        public Guid Id { get; init; }
        public string FileName { get; init; } = string.Empty;
        public long? FileSize { get; init; }
        public string? MimeType { get; init; }
        public string? Language { get; init; }
        public int PageCount { get; init; }
        public int ChunkCount { get; init; }
        public DateTime UploadedAt { get; init; }
        public DateTime? ProcessedAt { get; init; }
        public DocumentStatus Status { get; init; }
    }
}
