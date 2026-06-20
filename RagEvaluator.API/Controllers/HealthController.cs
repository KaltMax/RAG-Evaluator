using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly IHealthService _healthService;

        public HealthController(IHealthService healthService)
        {
            _healthService = healthService;
        }

        /// <summary>
        /// Checks if the RAG service is healthy and ready to process requests
        /// </summary>
        /// <returns>Health status indicating if Ollama services are available</returns>
        [HttpGet]
        public async Task<IActionResult> GetHealthAsync(CancellationToken cancellationToken)
        {
            var isReady = await _healthService.IsReadyAsync(cancellationToken);

            var response = new
            {
                status = isReady ? "healthy" : "degraded",
                ready = isReady,
                timestamp = DateTime.UtcNow
            };

            return isReady ? Ok(response) : StatusCode(503, response);
        }
    }
}
