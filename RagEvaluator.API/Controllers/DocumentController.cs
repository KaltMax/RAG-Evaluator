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
        private readonly IDocumentService _documentService;

        public DocumentController(
            ILoggerWrapper<DocumentController> logger,
            IRagService ragService,
            IDocumentService documentService)
        {
            _logger = logger;
            _ragService = ragService;
            _documentService = documentService;
        }

        /// <summary>
        /// Uploads a PDF document for RAG processing
        /// </summary>
        /// <param name="file">The PDF file to upload</param>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocumentAsync(IFormFile file)
        {
            try
            {
                if (file.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                if (!file.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Only PDF files are supported.");
                }

                _logger.LogInformation("Uploading document: {FileName}", file.FileName);

                using var stream = file.OpenReadStream();
                var result = await _ragService.ProcessDocumentAsync(stream, file.FileName);

                _logger.LogInformation("Document processed successfully: {DocumentId}", result.DocumentId);

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "RAG service not initialized");
                return StatusCode(503, new { error = "RAG service not available", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading document");
                return StatusCode(500, new { error = "Failed to process document", message = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetAllDocumentsAsync()
        {
            var documents = await _documentService.GetAllAsync();
            return Ok(documents);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetDocumentByIdAsync(Guid id)
        {
            var document = await _documentService.GetByIdAsync(id);
            if (document is null)
            {
                return NotFound();
            }
            return Ok(document);
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteDocumentAsync(Guid id)
        {
            var document = await _documentService.GetByIdAsync(id);
            if (document is null)
            {
                return NotFound();
            }

            await _documentService.DeleteAsync(id);
            _logger.LogInformation("Document deleted: {DocumentId}", id);
            return NoContent();
        }

        [HttpGet("{id}/chunks")]
        public async Task<IActionResult> GetDocumentChunksAsync(Guid id)
        {
            // TODO: Implement retrieval of document chunks by document ID
            return Ok();
        }
    }
}
