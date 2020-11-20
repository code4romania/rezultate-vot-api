using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using LazyCache;
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
        private readonly IAppCache _appCache;
        private readonly ITerritoryRepository _territoryRepository;

        public BallotsController(ILogger<BallotsController> logger, IResultsAggregator resultsAggregator, IAppCache appCache, ITerritoryRepository territoryRepository)
        {
            _logger = logger;
            _resultsAggregator = resultsAggregator;
            _appCache = appCache;
            _territoryRepository = territoryRepository;
        }

        [HttpGet("ballots")]
        public async Task<ActionResult<List<ElectionMeta>>> GetBallots()
        {
            var result = await _appCache.GetOrAddAsync(
                "ballots", () => _resultsAggregator.GetAllBallots(),
                DateTimeOffset.Now.AddMinutes(120));
            if (result.IsSuccess)
                return result.Value;
            return StatusCode(500, result.Error);
        }

        [HttpGet("ballots/{ballotId}/candidates")]
        public async Task<ActionResult<List<BasicCandidateInfo>>> GetCandidatesForBallot([FromQuery] ElectionResultsQuery query)
        {
            try
            {
                if (query.LocalityId == 0)
                    query.LocalityId = null;
                if (query.CountyId == 0)
                    query.CountyId = null;
                if (query.Round == 0)
                    query.Round = null;

                var result = await _appCache.GetOrAddAsync(
                    query.GetCacheKey(), () => _resultsAggregator.GetBallotCandidates(query),
                    DateTimeOffset.Now.AddMinutes(query.GetCacheDurationInMinutes()));
                return result.Value;
            }
            catch (Exception e)
            {
                _appCache.Remove(query.GetCacheKey());
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
                    query.LocalityId = null;
                if (query.CountyId == 0)
                    query.CountyId = null;
                if (query.Round == 0)
                    query.Round = null;

                var result = await _appCache.GetOrAddAsync(
                    query.GetCacheKey(), () => _resultsAggregator.GetBallotResults(query),
                    DateTimeOffset.Now.AddMinutes(query.GetCacheDurationInMinutes()));
                var newsFeed = await _resultsAggregator.GetNewsFeed(query, result.Value.Meta.ElectionId);
                result.Value.ElectionNews = newsFeed;
                return result.Value;
            }
            catch (Exception e)
            {
                _appCache.Remove(query.GetCacheKey());
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
        public async Task<ActionResult<List<LocationData>>> GetCountries()
        {
            try
            {
                var result = await _territoryRepository.GetCountries();
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
    }
}
