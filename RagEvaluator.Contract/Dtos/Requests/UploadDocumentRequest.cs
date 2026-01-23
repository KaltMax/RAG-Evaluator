using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace RagEvaluator.Contract.Dtos.Requests
{
    /// <summary>
    /// Represents a request to upload a document.
    /// </summary>
    public class UploadDocumentRequest
    {
        public required IFormFile File { get; set; }

        [Required]
        [RegularExpression("^(en|de)$", ErrorMessage = "Language must be 'en' or 'de'.")]
        public required string Language { get; set; }
    }
}
