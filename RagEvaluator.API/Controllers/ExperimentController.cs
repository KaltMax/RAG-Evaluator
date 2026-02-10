using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/experiments")]
    public class ExperimentController : ControllerBase
    {
        private readonly ILogger<ExperimentController> _logger;
        private readonly IExperimentService _experimentService;

        public ExperimentController(
            ILogger<ExperimentController> logger,
            IExperimentService experimentService)
        {
            _logger = logger;
            _experimentService = experimentService;
        }

        /// <summary>
        /// Creates a new experiment and starts background processing of all queries
        /// </summary>
        /// <param name="request">Experiment name, queries, and repeat count</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpPost]
        [ProducesResponseType(typeof(ExperimentSummaryResponse), StatusCodes.Status202Accepted)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateExperimentAsync([FromBody] CreateExperimentRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid experiment request received");
                return BadRequest(ModelState);
            }

            _logger.LogInformation("Creating experiment: {Name}", request.Name);

            var result = await _experimentService.CreateExperimentAsync(request, cancellationToken);

            _logger.LogInformation("Experiment created: {ExperimentId}", result.Id);

            return Accepted(result);
        }

        /// <summary>
        /// Retrieves all experiments with progress and configuration summary
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ExperimentSummaryResponse>), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<IEnumerable<ExperimentSummaryResponse>>> GetAllAsync(CancellationToken cancellationToken)
        {
            var experiments = await _experimentService.GetAllAsync(cancellationToken);
            return Ok(experiments);
        }

        /// <summary>
        /// Retrieves an experiment by ID with query groups and aggregated metrics
        /// </summary>
        /// <param name="id">The unique identifier of the experiment</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("{id}")]
        [ProducesResponseType(typeof(ExperimentResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<ActionResult<ExperimentResponse>> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            var experiment = await _experimentService.GetByIdAsync(id, cancellationToken);
            if (experiment is null)
            {
                _logger.LogWarning("Experiment not found: {ExperimentId}", id);
                return NotFound();
            }

            return Ok(experiment);
        }

        /// <summary>
        /// Deletes an experiment by ID (linked queries are preserved via SET NULL)
        /// </summary>
        /// <param name="id">The unique identifier of the experiment</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            var experiment = await _experimentService.GetByIdAsync(id, cancellationToken);
            if (experiment is null)
            {
                _logger.LogWarning("Experiment not found for deletion: {ExperimentId}", id);
                return NotFound();
            }

            await _experimentService.DeleteAsync(id, cancellationToken);
            _logger.LogInformation("Experiment deleted: {ExperimentId}", id);

            return NoContent();
        }
    }
}
