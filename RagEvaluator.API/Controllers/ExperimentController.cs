using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Logger;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/experiments")]
    public class ExperimentController : ControllerBase
    {
        private readonly ILoggerWrapper<ExperimentController> _logger;
        private readonly IExperimentService _experimentService;

        public ExperimentController(
            ILoggerWrapper<ExperimentController> logger,
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
        public async Task<IActionResult> CreateExperimentAsync([FromBody] CreateExperimentRequest request, CancellationToken cancellationToken)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating experiment");
                return StatusCode(500, new { error = "Failed to create experiment", message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves all experiments with progress and configuration summary
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetAllAsync(CancellationToken cancellationToken)
        {
            try
            {
                var experiments = await _experimentService.GetAllAsync(cancellationToken);
                return Ok(experiments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving experiments");
                return StatusCode(500, new { error = "Failed to retrieve experiments", message = ex.Message });
            }
        }

        /// <summary>
        /// Retrieves an experiment by ID with query groups and aggregated metrics
        /// </summary>
        /// <param name="id">The unique identifier of the experiment</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetByIdAsync(Guid id, CancellationToken cancellationToken)
        {
            try
            {
                var experiment = await _experimentService.GetByIdAsync(id, cancellationToken);
                if (experiment is null)
                {
                    _logger.LogWarning("Experiment not found: {ExperimentId}", id);
                    return NotFound();
                }

                return Ok(experiment);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving experiment: {ExperimentId}", id);
                return StatusCode(500, new { error = "Failed to retrieve experiment", message = ex.Message });
            }
        }

        /// <summary>
        /// Deletes an experiment by ID (linked queries are preserved via SET NULL)
        /// </summary>
        /// <param name="id">The unique identifier of the experiment</param>
        /// <param name="cancellationToken">Cancellation token</param>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAsync(Guid id, CancellationToken cancellationToken)
        {
            try
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
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting experiment: {ExperimentId}", id);
                return StatusCode(500, new { error = "Failed to delete experiment", message = ex.Message });
            }
        }
    }
}
