using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Response;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ElectionResults.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class BallotsController : ControllerBase
    {
        private readonly ILogger<BallotsController> _logger;
        private readonly IResultsAggregator _resultsAggregator;

        public BallotsController(ILogger<BallotsController> logger, IResultsAggregator resultsAggregator)
        {
            _logger = logger;
            _resultsAggregator = resultsAggregator;
        }

        [HttpGet("ballots")]
        public async Task<ActionResult<List<ElectionMeta>>> GetBallots()
        {
            var result = await _resultsAggregator.GetAllBallots();
            if (result.IsSuccess)
                return result.Value;
            return StatusCode(500, result.Error);
        }
    }
}
