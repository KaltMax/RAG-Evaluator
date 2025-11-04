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
        private readonly IDocumentService _documentService;

        public DocumentController(ILoggerWrapper<DocumentController> logger, IDocumentService documentService)
        {
            _logger = logger;
            _documentService = documentService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadDocumentAsync()
        {
            return Ok();
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
