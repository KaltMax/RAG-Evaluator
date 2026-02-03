using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
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
        /// <param name="request">The upload request containing file and language</param>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        public async Task<IActionResult> UploadDocumentAsync([FromForm] UploadDocumentRequest request)
        {
            try
            {
                if (request.File.Length == 0)
                {
                    return BadRequest("No file uploaded.");
                }

                if (!request.File.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    return BadRequest("Only PDF files are supported.");
                }

                _logger.LogInformation("Uploading document: {FileName}, Language: {Language}", request.File.FileName, request.Language);

                using var stream = request.File.OpenReadStream();
                var result = await _ragService.ProcessDocumentAsync(stream, request.File.FileName, request.File.ContentType, request.Language);

                _logger.LogInformation("Document processed successfully: {DocumentId}", result.Id);

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

        /// <summary>
        /// Retrieves all documents
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllDocumentsAsync()
        {
            var documents = await _documentService.GetAllAsync();
            return Ok(documents);
        }

        /// <summary>
        /// Retrieves a specific document by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
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

        /// <summary>
        /// Deletes a document and its associated chunks
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
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

        /// <summary>
        /// Downloads the original PDF file for a document
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
        [HttpGet("{id}/download")]
        public async Task<IActionResult> DownloadDocumentAsync(Guid id)
        {
            var fileInfo = await _documentService.GetDocumentFileInfoAsync(id);
            if (fileInfo is null)
            {
                return NotFound();
            }

            var fileBytes = await System.IO.File.ReadAllBytesAsync(fileInfo.FilePath);
            return File(fileBytes, fileInfo.MimeType, fileInfo.FileName);
        }

        /// <summary>
        /// Retrieves all chunks for a specific document
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
        [HttpGet("{id}/chunks")]
        public async Task<IActionResult> GetDocumentChunksAsync(Guid id)
        {
            var document = await _documentService.GetByIdAsync(id);
            if (document is null)
            {
                return NotFound();
            }

            var chunks = await _documentService.GetChunksByDocumentIdAsync(id);
            return Ok(chunks);
        }
    }
}
