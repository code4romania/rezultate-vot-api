using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectionResults.API.Options;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;

namespace ElectionResults.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class BallotsController : ControllerBase
    {
        private readonly IResultsAggregator _resultsAggregator;
        private readonly ITerritoryRepository _territoryRepository;
        private readonly MemoryCacheSettings _cacheSettings;

        public BallotsController(IResultsAggregator resultsAggregator,
            ITerritoryRepository territoryRepository,
            IOptions<MemoryCacheSettings> cacheSettings)
        {
            _resultsAggregator = resultsAggregator;
            _territoryRepository = territoryRepository;
            _cacheSettings = cacheSettings.Value;
        }

        [HttpGet("ballots")]
        public async Task<ActionResult<List<ElectionMeta>>> GetBallots()
        {
            var result = await  _resultsAggregator.GetAllBallots();

            if (result.IsSuccess)
            {
                return result.Value;
            }

            return StatusCode(500, result.Error);
        }

        [HttpGet("ballots/{ballotId}/candidates")]
        public async Task<ActionResult<List<PartyList>>> GetCandidatesForBallot([FromQuery] ElectionResultsQuery query, int ballotId)
        {
            try
            {
                query.BallotId = ballotId;

                if (query.LocalityId == 0)
                {
                    query.LocalityId = null;
                }

                if (query.CountyId == 0)
                {
                    query.CountyId = null;
                }

                if (query.Round == 0)
                {
                    query.Round = null;
                }

                var result = await _resultsAggregator.GetBallotCandidates(query);

                return result.Value;
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while retrieving voter turnout stats");
                return StatusCode(500, e.StackTrace);
            }
        }

        [HttpGet("ballot")]
        public async Task<ActionResult<ElectionResponse>> GetBallot([FromQuery] ElectionResultsQuery query)
        {
            try
            {
                if (query.LocalityId == 0)
                {
                    query.LocalityId = null;
                }

                if (query.CountyId == 0)
                {
                    query.CountyId = null;
                }

                if (query.Round == 0)
                {
                    query.Round = null;
                }

                var expiration = GetExpirationDate(query);

                var result = await _resultsAggregator.GetBallotResults(query);

                var newsFeed = await _resultsAggregator.GetNewsFeed(query, result.Value.Meta.ElectionId);
                result.Value.ElectionNews = newsFeed;

                return result.Value;
            }
            catch (Exception e)
            {
                Log.LogError(e, "Exception encountered while retrieving voter turnout stats");
                return StatusCode(500, e.StackTrace);
            }
        }

        [HttpGet("counties")]
        public async Task<ActionResult<List<LocationData>>> GetCounties()
        {
            try
            {
                var countiesResult = await _territoryRepository.GetCounties();
                if (countiesResult.IsSuccess)
                {
                    return countiesResult.Value.Select(c => new LocationData
                    {
                        Id = c.CountyId,
                        Name = c.Name
                    }).ToList();
                }

                return StatusCode(500, countiesResult.Error);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("localities")]
        public async Task<ActionResult<List<LocationData>>> GetLocalities([FromQuery] int? countyId, int? ballotId)
        {
            try
            {
                var result = await _territoryRepository.GetLocalities(countyId, ballotId);
                if (result.IsSuccess)
                {
                    return result.Value.Select(c => new LocationData
                    {
                        Id = c.LocalityId,
                        Name = c.Name,
                        CountyId = c.CountyId
                    }).ToList();
                }

                return StatusCode(500, result.Error);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        [HttpGet("countries")]
        public async Task<ActionResult<List<LocationData>>> GetCountries([FromQuery] int? ballotId)
        {
            try
            {
                var result = await _territoryRepository.GetCountries(ballotId);
                if (result.IsSuccess)
                {
                    return result.Value.Select(c => new LocationData
                    {
                        Id = c.Id,
                        Name = c.Name
                    }).ToList();
                }

                return StatusCode(500, result.Error);
            }
            catch (Exception e)
            {
                return StatusCode(500, e.Message);
            }
        }

        private DateTimeOffset GetExpirationDate(ElectionResultsQuery electionResultsQuery)
        {
            if (electionResultsQuery.BallotId <= 110) // ballot older than parliament elections in 2020
            {
                return DateTimeOffset.Now.AddDays(1);
            }
            return DateTimeOffset.Now.AddMinutes(_cacheSettings.ResultsCacheInMinutes);
        }
    }
}
