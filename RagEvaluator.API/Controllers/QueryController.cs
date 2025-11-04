using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Logger;
using RagEvaluator.Contract.Requests;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/query")]
    public class QueryController : ControllerBase
    {
        private readonly ILoggerWrapper<QueryController> _logger;
        private readonly IRagService _ragService;

        public QueryController(ILoggerWrapper<QueryController> logger, IRagService ragService)
        {
            _logger = logger;
            _ragService = ragService;
        }

        /// <summary>
        /// Asks a question using RAG (Retrieval-Augmented Generation)
        /// </summary>
        /// <param name="request">Question and search parameters</param>
        [HttpPost]
        public async Task<IActionResult> QueryAsync([FromBody] AskQuestionRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return BadRequest(ModelState);
                }

                _logger.LogInformation($"Processing query: {request.Question}");

                var result = await _ragService.AskQuestionAsync(request.Question, request.TopK);

                _logger.LogInformation($"Query processed successfully: {result.QueryId}");

                return Ok(result);
            }
            catch (InvalidOperationException ex)
            {
                _logger.LogError($"RAG service not initialized: {ex.Message}");
                return StatusCode(503, new { error = "RAG service not available", message = ex.Message });
            }
            catch (Exception ex)
            {
                _logger.LogError($"Error processing query: {ex.Message}");
                return StatusCode(500, new { error = "Failed to process query", message = ex.Message });
            }
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetQueryHistoryAsync()
        {
            return Ok();
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetQueryByIdAsync(Guid id)
        {
            return Ok();
        }
    }
}
