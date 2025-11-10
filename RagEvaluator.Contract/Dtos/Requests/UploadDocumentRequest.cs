using Microsoft.AspNetCore.Http;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Represents a request to upload a document, including the file and an optional description.
    /// </summary>
    public class UploadDocumentRequest
    {
        public required IFormFile File { get; set; }
        public string? Description { get; set; }
    }
}
