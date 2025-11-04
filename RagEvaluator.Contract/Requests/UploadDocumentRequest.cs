using Microsoft.AspNetCore.Http;

namespace RagEvaluator.Contract.Requests
{
    public class UploadDocumentRequest
    {
        public required IFormFile File { get; set; }
        public string? Description { get; set; }
    }
}
