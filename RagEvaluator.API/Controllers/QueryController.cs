using Microsoft.AspNetCore.Mvc;
using RagEvaluator.Application.Services.Interfaces;
using RagEvaluator.Contract.Logger;

namespace RagEvaluator.API.Controllers
{
    [ApiController]
    [Route("api/query")]
    public class QueryController : ControllerBase
    {
        private readonly ILoggerWrapper<QueryController> _logger;
        private readonly IQueryService _queryService;

        public QueryController(ILoggerWrapper<QueryController> logger, IQueryService queryService)
        {
            _logger = logger;
            _queryService = queryService;
        }

        [HttpPost]
        public async Task<IActionResult> QueryAsync()
        {
            return Ok();
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
