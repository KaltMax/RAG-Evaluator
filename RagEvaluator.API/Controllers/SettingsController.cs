using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Dtos.Requests;
using RagEvaluator.Contract.Logger;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/settings")]
    public class SettingsController : ControllerBase
    {
        private readonly ILoggerWrapper<SettingsController> _logger;
        private readonly ISettingsService _settingsService;
        
        public SettingsController(ILoggerWrapper<SettingsController> logger, ISettingsService settingsService)
        {
            _logger = logger;
            _settingsService = settingsService;
        }

        /// <summary>
        /// Returns the current runtime RAG configuration and available options.
        /// </summary>
        [HttpGet]
        public IActionResult GetSettings()
        {
            try
            {
                var settings = _settingsService.GetSettings();
                return Ok(settings);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving settings");
                return StatusCode(500, new { error = "Failed to retrieve settings", message = ex.Message });
            }
        }

        /// <summary>
        /// Updates runtime RAG configuration. Only provided (non-null) fields are applied.
        /// Changing the embedding model triggers reinitialization of the embedding service.
        /// </summary>
        /// <param name="request">The settings to update. Only non-null fields will be applied.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        [HttpPatch]
        public async Task<IActionResult> UpdateSettings([FromBody] UpdateSettingsRequest request, CancellationToken cancellationToken)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    _logger.LogWarning("Invalid settings request received");
                    return BadRequest(ModelState);
                }

                var response = await _settingsService.UpdateSettingsAsync(request);
                return Ok(response);
            }
            catch (ArgumentException ex)
            {
                _logger.LogError(ex, "Invalid settings update request");
                return BadRequest(new { error = "Invalid settings update request", message = ex.Message });
            }
        }
    }
}
