using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;
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
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpPost("upload")]
        [Consumes("multipart/form-data")]
        [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<DocumentResponse>> UploadDocumentAsync([FromForm] UploadDocumentRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid upload request received");
                    return BadRequest(ModelState);
                }

                if (request.File.Length == 0)
                {
                    _logger.LogWarning("Empty file uploaded");
                    return BadRequest("No file uploaded.");
                }

                if (!request.File.FileName.EndsWith(".pdf", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogWarning("Unsupported file type uploaded: {FileName}", request.File.FileName);
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
        [ProducesResponseType(typeof(IEnumerable<DocumentResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DocumentResponse>>> GetAllDocumentsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var documents = await _documentService.GetAllAsync(cancellationToken);
                return Ok(documents);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving documents");
                return StatusCode(500, new { error = "Failed to retrieve documents", message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves a specific document by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(DocumentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<DocumentResponse>> GetDocumentByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _documentService.GetByIdAsync(id, cancellationToken);
                if (document is null)
                {
                    _logger.LogWarning("Document not found: {DocumentId}", id);
                    return NotFound();
                }

                return Ok(document);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving document with ID: {DocumentId}", id);
                return StatusCode(500, new { error = "Failed to retrieve document", message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes a document and its associated chunks
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteDocumentAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _documentService.GetByIdAsync(id, cancellationToken);
                if (document is null)
                {
                    _logger.LogWarning("Attempted to delete non-existent document: {DocumentId}", id);
                    return NotFound();
                }

                await _documentService.DeleteAsync(id, cancellationToken);
                _logger.LogInformation("Document deleted: {DocumentId}", id);
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document with ID: {DocumentId}", id);
                return StatusCode(500, new { error = "Failed to delete document", message = ex.Message });
            }
        }

        /// <summary>
        /// Downloads the original PDF file for a document
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("{id}/download")]
        [ProducesResponseType(typeof(FileResult), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DownloadDocumentAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var fileInfo = await _documentService.GetDocumentFileInfoAsync(id, cancellationToken);
                if (fileInfo is null)
                {
                    _logger.LogWarning("Attempted to download non-existent document: {DocumentId}", id);
                    return NotFound();
                }

                var fileBytes = await System.IO.File.ReadAllBytesAsync(fileInfo.FilePath, cancellationToken);
                return File(fileBytes, fileInfo.MimeType, fileInfo.FileName);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error downloading document with ID: {DocumentId}", id);
                return StatusCode(500, new { error = "Failed to download document", message = ex.Message });
            }
        }

        /// <summary>
        /// Reprocesses all completed documents by deleting existing chunks and re-chunking + re-embedding with the current configuration.
        /// </summary>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpPost("reprocess")]
        [ProducesResponseType(typeof(ReprocessResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<ReprocessResponse>> ReprocessDocumentsAsync(CancellationToken cancellationToken)
        {
            try
            {
                _logger.LogInformation("Reprocessing all documents with current configuration");
                var result = await _documentService.ReprocessAllDocumentsAsync(cancellationToken);
                _logger.LogInformation("Reprocessing complete: {Documents} documents, {Chunks} chunks", result.DocumentsProcessed, result.TotalChunksCreated);
                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError(ex, "Embedding service not available for reprocessing");
                return StatusCode(503, new { error = "Embedding service not available", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reprocessing documents");
                return StatusCode(500, new { error = "Failed to reprocess documents", message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all chunks for a specific document
        /// </summary>
        /// <param name="id">The unique identifier of the document</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("{id}/chunks")]
        [ProducesResponseType(typeof(IEnumerable<DocumentChunkResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<DocumentChunkResponse>>> GetDocumentChunksAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var document = await _documentService.GetByIdAsync(id, cancellationToken);
                if (document is null)
                {
                    _logger.LogWarning("Attempted to retrieve chunks for non-existent document: {DocumentId}", id);
                    return NotFound();
                }

                var chunks = await _documentService.GetChunksByDocumentIdAsync(id, cancellationToken);
                return Ok(chunks);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving chunks for document with ID: {DocumentId}", id);
                return StatusCode(500, new { error = "Failed to retrieve document chunks", message = ex.Message });
            }
        }
    }
}
