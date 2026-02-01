using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Logger;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/query")]
    public class QueryController : ControllerBase
    {
        private readonly ILoggerWrapper<QueryController> _logger;
        private readonly IRagService _ragService;
        private readonly IQueryService _queryService;

        public QueryController(
            ILoggerWrapper<QueryController> logger,
            IRagService ragService,
            IQueryService queryService)
        {
            _logger = logger;
            _ragService = ragService;
            _queryService = queryService;
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

                var result = await _ragService.AskQuestionAsync(request);

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

        /// <summary>
        /// Retrieves the history of all executed queries
        /// </summary>
        [HttpGet("history")]
        public async Task<IActionResult> GetQueryHistoryAsync()
        {
            var queries = await _queryService.GetAllAsync();
            return Ok(queries);
        }

        /// <summary>
        /// Retrieves a specific query by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the query</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetQueryByIdAsync(Guid id)
        {
            var query = await _queryService.GetByIdAsync(id);
            if (query is null)
            {
                return NotFound();
            }
            return Ok(query);
        }
    }
}
