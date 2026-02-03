using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        private readonly IRagService _ragService;

        public HealthController(IRagService ragService)
        {
            _ragService = ragService;
        }

        /// <summary>
        /// Checks if the RAG service is healthy and ready to process requests
        /// </summary>
        /// <returns>Health status indicating if Ollama services are available</returns>
        [HttpGet]
        public async Task<IActionResult> GetHealthAsync()
        {
            var isReady = await _ragService.IsInitializedAsync();

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
