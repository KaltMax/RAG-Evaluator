using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Dtos.Responses;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly ILogger<SettingsController> _logger;
        private readonly ISettingsService _settingsService;

        public SettingsController(ILogger<SettingsController> logger, ISettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Returns the current runtime RAG configuration and available options.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status500InternalServerError)]
        public ActionResult<SettingsResponse> GetSettings()
        {
            var settings = _settingsService.GetSettings();
            return Ok(settings);
        }

        /// <summary>
        /// Updates runtime RAG configuration. Only provided (non-null) fields are applied.
        /// Changing the embedding model triggers reinitialization of the embedding service.
        /// </summary>
        /// <param name="request">The settings to update. Only non-null fields will be applied.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPatch]
        [ProducesResponseType(typeof(SettingsResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ProblemDetails), StatusCodes.Status400BadRequest)]
        public async Task<ActionResult<SettingsResponse>> UpdateSettings([FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken)
        {
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid settings request received");
                return BadRequest(ModelState);
            }

            var response = await _settingsService.UpdateSettingsAsync(request);
            return Ok(response);
        }
    }
}
