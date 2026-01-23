namespace RagEvaluator.Contract.Dtos.Responses
{
    /// <summary>
    /// Contains file information needed for document downloads.
    /// </summary>
    public class DocumentFileInfo
    {
        public required string FilePath { get; set; }
        public required string FileName { get; set; }
        public required string MimeType { get; set; }
    }
}
