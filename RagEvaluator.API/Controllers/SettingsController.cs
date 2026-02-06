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
            return Ok(_settingsService.GetSettings());
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
                _logger.LogError("Invalid settings update request: {Message}", ex.Message);
                return BadRequest(new { errors = ex.Message });
            }
        }
    }
}
