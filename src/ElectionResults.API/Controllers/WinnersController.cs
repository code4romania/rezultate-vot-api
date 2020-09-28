using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace ElectionResults.API.Controllers
{
    [ApiController]
    [Route("api/winners")]
    public class WinnersController : ControllerBase
    {
        private readonly IWinnersAggregator _winnersAggregator;

        public WinnersController(IWinnersAggregator winnersAggregator)
        {
            _winnersAggregator = winnersAggregator;
        }

        [HttpGet("countries")]
        public async Task<ActionResult<List<ElectionMapWinner>>> GetWinnersForCountry([FromQuery] int ballotId)
        {
            try
            {
                var result = await _winnersAggregator.GetCountryWinners(ballotId);
                if (result.IsSuccess)
                    return result.Value;
                throw new Exception(result.Error);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while country winners");
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("counties")]
        public async Task<ActionResult<List<ElectionMapWinner>>> GetWinnersForCounty([FromQuery] int ballotId)
        {
            try
            {
                var result = await _winnersAggregator.GetCountyWinners(ballotId);
                if (result.IsSuccess)
                    return result.Value;
                throw new Exception(result.Error);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while county winners");
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("localities")]
        public async Task<ActionResult<List<ElectionMapWinner>>> GetWinnersForLocality([FromQuery] int ballotId, int countyId)
        {
            try
            {
                var result = await _winnersAggregator.GetLocalityWinnersByCounty(ballotId, countyId);
                if (result.IsSuccess)
                    return result.Value;
                throw new Exception(result.Error);
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while locality winners");
                return StatusCode(500, e.Message);
            }
        }
    }
}
