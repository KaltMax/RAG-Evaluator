namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Represents the metadata and status information for a document that has been uploaded.
    /// </summary>
    public class DocumentResponse
    {
        public required Guid Id { get; set; }
        public required string FileName { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
        public string? Language { get; set; }
        public required string Course { get; set; }
        public int PageCount { get; set; }
        public int ChunkCount { get; set; }
        public DateTime UploadedAt { get; set; }
        public DateTime? ProcessedAt { get; set; }
        public string Status { get; set; } = string.Empty;
    }
}
