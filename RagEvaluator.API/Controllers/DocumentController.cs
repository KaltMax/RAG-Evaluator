using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Logger;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/documents")]
    public class DocumentController : ControllerBase
    {
        private readonly ILoggerWrapper<DocumentController> _logger;
        private readonly IRagService _ragService;

        public DocumentController(ILoggerWrapper<DocumentController> logger, IRagService ragService)
        {
            _logger = logger;
            _ragService = ragService;
        }

        /// <summary>
        /// Uploads a PDF document for RAG processing
        /// </summary>
        /// <param name="file">The PDF file to upload</param>
        /// <param name="description">Optional description of the document</param>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocumentAsync(IFormFile file, [FromForm] string? description = null)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Only PDF files are supported.");
                }

                _logger.LogInformation($"Uploading document: {file.FileName}");

                using var stream = file.OpenReadStream();
                var result = await _ragService.ProcessDocumentAsync(stream, file.FileName, description);

                _logger.LogInformation($"Document processed successfully: {result.DocumentId}");

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"RAG service not initialized: {ex.Message}");
                return StatusCode(503, new { error = "RAG service not available", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error uploading document: {ex.Message}");
                return StatusCode(500, new { error = "Failed to process document", message = ex.Message });
            }
        }

        [HttpGet()]
        public async Task<IActionResult> GetAllDocumentsAsync()
        {
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentByIdAsync(Guid id)
        {
            return Ok();
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocumentAsync(Guid id)
        {
            return Ok();
        }

        [HttpGet("{id}/chunks")]
        public async Task<IActionResult> GetDocumentChunksAsync(Guid id)
        {
            return Ok();
        }
    }
}
