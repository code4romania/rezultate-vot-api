using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using Microsoft.AspNetCore.Mvc;
using ZiggyCreatures.Caching.Fusion;

namespace ElectionResults.API.Controllers
{
    [ApiController]
    [Route("api")]
    public class BallotsController : ControllerBase
    {
        private readonly IResultsAggregator _resultsAggregator;
        private readonly ITerritoryRepository _territoryRepository;
        private readonly IFusionCache _fusionCache;

        public BallotsController(IResultsAggregator resultsAggregator,
            ITerritoryRepository territoryRepository,
            IFusionCache fusionCache)
        {
            _resultsAggregator = resultsAggregator;
            _territoryRepository = territoryRepository;
            _fusionCache = fusionCache;
        }

        [HttpGet("ballots")]
        public async Task<ActionResult<List<ElectionMeta>>> GetBallots()
        {
            var result = await _fusionCache.GetOrSetAsync<Result<List<ElectionMeta>>>("ballots", async (ctx, ct) =>
            {
                var result = await _resultsAggregator.GetAllBallots();
                ctx.Options.Duration = TimeSpan.FromMinutes(result.IsFailure ? 1 : 10);
                return result;
            });

            if (result.IsSuccess)
            {
                return result.Value;
            }

            return StatusCode(500, result.Error);
        }

        [HttpGet("ballots/{ballotId}/candidates")]
        public async Task<ActionResult<List<PartyList>>> GetCandidatesForBallot([FromQuery] ElectionResultsQuery query,
            int ballotId)
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

                var result = await _fusionCache.GetOrSetAsync<Result<List<PartyList>>>(query.GetCacheKey("candidates"),
                    async (ctx, ct) =>
                    {
                        var result = await _resultsAggregator.GetBallotCandidates(query);
                        ctx.Options.Duration = TimeSpan.FromMinutes(result.IsFailure ? 1 : 10);
                        return result;
                    });

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

                var result = await _fusionCache.GetOrSetAsync<Result<ElectionResponse>>(query.GetCacheKey("ballot"),
                    async (ctx, ct) =>
                    {
                        var result = await _resultsAggregator.GetBallotResults(query);
                        ctx.Options.Duration =
                            TimeSpan.FromMinutes(result.IsFailure ? 1 : query.BallotId <= 113 ? 1440 : 10);
                        return result;
                    });

                if (result.IsFailure)
                {
                    return StatusCode(500, result.Error);
                }

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
            var result = await _fusionCache.GetOrSetAsync<Result<List<LocationData>>>("counties",
                async (ctx, ct) =>
                {
                    var countiesResult = await _territoryRepository.GetCounties();
                    ctx.Options.Duration = TimeSpan.FromMinutes(countiesResult.IsFailure ? 1 : 1440);

                    if (countiesResult.IsFailure)
                    {
                        return Result.Failure<List<LocationData>>(countiesResult.Error);
                    }

                    return countiesResult.Value.Select(c => new LocationData
                    {
                        Id = c.CountyId,
                        Name = c.Name
                    }).ToList();
                });

            if (result.IsFailure)
            {
                return StatusCode(500, result.Error);
            }

            return result.Value;
        }

        [HttpGet("localities")]
        public async Task<ActionResult<List<LocationData>>> GetLocalities([FromQuery] int? countyId, int? ballotId)
        {
            var result = await _fusionCache.GetOrSetAsync<Result<List<LocationData>>>(
                $"localities-{ballotId}-{countyId}",
                async (ctx, ct) =>
                {
                    var localitiesResult = await _territoryRepository.GetLocalities(countyId, ballotId);
                    ctx.Options.Duration = TimeSpan.FromMinutes(localitiesResult.IsFailure ? 1 : 1440);

                    if (localitiesResult.IsFailure)
                    {
                        return Result.Failure<List<LocationData>>(localitiesResult.Error);
                    }

                    return localitiesResult.Value.Select(c => new LocationData
                    {
                        Id = c.LocalityId,
                        Name = c.Name,
                        CountyId = c.CountyId
                    }).ToList();
                });

            if (result.IsFailure)
            {
                return StatusCode(500, result.Error);
            }

            return result.Value;
        }

        [HttpGet("countries")]
        public async Task<ActionResult<List<LocationData>>> GetCountries([FromQuery] int? ballotId)
        {
            var result = await _fusionCache.GetOrSetAsync<Result<List<LocationData>>>(
                $"countries-{ballotId}",
                async (ctx, ct) =>
                {
                    var countriesResult = await _territoryRepository.GetCountries(ballotId);
                    ctx.Options.Duration = TimeSpan.FromMinutes(countriesResult.IsFailure ? 1 : 1440);

                    if (countriesResult.IsFailure)
                    {
                        return Result.Failure<List<LocationData>>(countriesResult.Error);
                    }

                    return countriesResult.Value.Select(c => new LocationData
                    {
                        Id = c.Id,
                        Name = c.Name
                    }).ToList();
                });

            if (result.IsFailure)
            {
                return StatusCode(500, result.Error);
            }

            return result.Value;
        }
    }
}