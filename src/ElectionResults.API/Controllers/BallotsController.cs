using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace ElectionResults.API.Controllers
{
    [ApiController]
    [Route("api/ballots")]
    public class BallotsController : ControllerBase
    {
        private readonly ILogger<BallotsController> _logger;

        public BallotsController(ILogger<BallotsController> logger)
        {
            _logger = logger;
        }

        [HttpGet]
        public string Get()
        {
            return "Hello";
        }
    }
}
