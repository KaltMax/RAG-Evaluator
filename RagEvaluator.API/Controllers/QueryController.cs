using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/query")]
    public class QueryController : ControllerBase
    {
        private readonly ILogger<QueryController> _logger;
        private readonly IRagService _ragService;
        private readonly IQueryService _queryService;

        public QueryController(
            ILogger<QueryController> logger,
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
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpPost]
        [ProducesResponseType(typeof(QueryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status503ServiceUnavailable)]
        public async Task<ActionResult<QueryResponse>> QueryAsync([FromBody] AskQuestionRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid query request received");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Processing query: {Question}", request.Question);

            var result = await _ragService.AskQuestionAsync(request, cancellationToken);

            _logger.LogInformation("Query processed successfully: {QueryId}", result.QueryId);

            return Ok(result);
        }

        /// <summary>
        /// Retrieves the history of all executed queries
        /// </summary>
        [HttpGet("history")]
        [ProducesResponseType(typeof(IEnumerable<QueryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<QueryResponse>>> GetQueryHistoryAsync(CancellationToken cancellationToken)
        {
            var queries = await _queryService.GetAllAsync(cancellationToken);
            return Ok(queries);
        }

        /// <summary>
        /// Retrieves a specific query by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(QueryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QueryResponse>> GetQueryByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var query = await _queryService.GetByIdAsync(id, cancellationToken);
            if (query is null)
            {
                _logger.LogWarning("Query not found: {QueryId}", id);
                return NotFound();
            }

            return Ok(query);
        }

        /// <summary>
        /// Annotates query results with relevance labels and calculates metrics
        /// </summary>
        /// <param name="queryId">The unique identifier of the query</param>
        /// <param name="request">The relevance annotations for query results</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpPatch("{queryId}/results")]
        [ProducesResponseType(typeof(QueryResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<QueryResponse>> AnnotateResultsAsync(Guid queryId, [FromBody] AnnotateResultsRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(ModelState);
            }

            var query = await _queryService.GetByIdAsync(queryId, cancellationToken);
            if (query is null)
            {
                _logger.LogWarning("Query not found for annotation: {QueryId}", queryId);
                return NotFound();
            }

            await _queryService.AnnotateResultsAsync(queryId, request.Annotations, request.ResponseQuality, request.HasLanguageSwitching, request.RelevantDocumentIds, cancellationToken);

            _logger.LogInformation("Query results annotated and metrics calculated: {QueryId}", queryId);

            var updatedQuery = await _queryService.GetByIdAsync(queryId, cancellationToken);
            return Ok(updatedQuery);
        }

        /// <summary>
        /// Deletes a query by its ID
        /// </summary>
        /// <param name="id">The unique identifier of the query</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteQueryAsync(Guid id, CancellationToken cancellationToken)
        {
            var query = await _queryService.GetByIdAsync(id, cancellationToken);
            if (query is null)
            {
                _logger.LogWarning("Query not found for deletion: {QueryId}", id);
                return NotFound();
            }

            await _queryService.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Query deleted: {QueryId}", id);

            return NoContent();
        }
    }
}
