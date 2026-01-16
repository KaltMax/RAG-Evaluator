using Microsoft.AspNetCore.Http;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Represents a request to upload a document.
    /// </summary>
    public class UploadDocumentRequest
    {
        public required IFormFile File { get; set; }
    }
}
