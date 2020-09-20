using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

namespace ElectionResults.Core.Elections
{
    public class ResultsAggregator : IResultsAggregator
    {
        private readonly IServiceProvider _serviceProvider;

        public ResultsAggregator(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }
        public async Task<Result<List<ElectionMeta>>> GetAllBallots()
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var elections = await dbContext.Elections.Include(e => e.Ballots).ToListAsync();
                var metas = (from election in elections.OrderByDescending(e => e.Date)
                from electionBallot in election.Ballots
                select new ElectionMeta
                {
                    Date = electionBallot.Date,
                    Title = election.Name ?? election.Subtitle ?? electionBallot.Subtitle,
                    Ballot = electionBallot.Name ?? election.Subtitle ?? electionBallot.Subtitle,
                    ElectionId = election.ElectionId,
                    Type = electionBallot.BallotType,
                    Subtitle = election.Subtitle,
                    BallotId = electionBallot.BallotId,
                    Round = electionBallot.Round == 0 ? null : electionBallot.Round
                }).ToList();
                return Result.Success(metas);
            }
        }

        public async Task<Result<ElectionResponse>> GetOldResults(ElectionResultsQuery query)
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var electionResponse = new ElectionResponse();
                var ballot = dbContext.Ballots.FirstOrDefault(e => e.BallotId == query.BallotId);

                var results = new ElectionResultsResponse();

                var resultsQuery = dbContext.CandidateResults.Where(er =>
                        er.BallotId == ballot.BallotId &&
                        er.Division == query.Division &&
                        er.CountyId == query.CountyId &&
                        er.LocalityId == query.LocalityId);

                var candidates = await resultsQuery.ToListAsync();
                var divisionTurnout = await dbContext.Turnouts
                    .FirstOrDefaultAsync(t =>
                        t.BallotId == ballot.BallotId &&
                        t.CountyId == query.CountyId &&
                        t.LocalityId == query.LocalityId);
                var electionTurnout = await dbContext.Turnouts
                    .FirstOrDefaultAsync(t =>
                        t.BallotId == ballot.BallotId && t.CountyId == query.CountyId && t.LocalityId == query.LocalityId);
                if (electionTurnout == null)
                {
                    var json = JsonConvert.SerializeObject(query, new JsonSerializerSettings
                    {
                        NullValueHandling = NullValueHandling.Ignore,
                        ReferenceLoopHandling = ReferenceLoopHandling.Ignore
                    });
                    throw new Exception($"No turnout found for this query: {json}");
                }
                results.NullVotes = electionTurnout.NullVotes;
                results.TotalVotes = electionTurnout.TotalVotes;
                results.ValidVotes = electionTurnout.ValidVotes;
                results.EligibleVoters = electionTurnout.EligibleVoters;
                results.TotalSeats = electionTurnout.TotalSeats;
                results.VotesByMail = electionTurnout.VotesByMail != 0 ? electionTurnout.VotesByMail : (int?)null;
                if (ballot.BallotType == BallotType.Referendum)
                {
                    results.Candidates = new List<CandidateResponse>
                    {
                        new CandidateResponse
                        {
                            Name = "DA",
                            ShortName = "DA",
                            Votes = candidates.FirstOrDefault().YesVotes,
                        },
                        new CandidateResponse
                        {
                            Name = "NU",
                            ShortName = "NU",
                            Votes = candidates.FirstOrDefault().NoVotes,
                        }
                    };
                }
                else
                {
                    results.Candidates = candidates.Select(c => new CandidateResponse
                    {
                        ShortName = c.ShortName,
                        Name = c.Name,
                        Votes = c.Votes,
                        PartyColor = c.Color,
                        PartyLogo = c.Logo
                    }).OrderByDescending(c => c.Votes).ToList();
                }
                electionResponse.Results = results;
                electionResponse.Turnout = new ElectionTurnout
                {
                    TotalVotes = divisionTurnout.TotalVotes,
                    EligibleVoters = divisionTurnout.EligibleVoters,
                    /*Breakdown = new ElectionTurnoutBreakdown
                    {
                        Categories = new List<TurnoutCategory>
                        {
                            new TurnoutCategory
                            {
                                Type = VoteType.PermanentLists
                            }
                        }
                    }*/
                };
                electionResponse.Scope = new ElectionScope
                {
                    Type = query.Division,
                    City = dbContext.Localities.FirstOrDefault(l => l.LocalityId == query.LocalityId)?.Name,
                    County = dbContext.Counties.FirstOrDefault(l => l.CountyId == query.CountyId)?.Name
                };
                electionResponse.Meta = new ElectionMeta
                {
                    Date = ballot.Date,
                    Type = ballot.BallotType,
                    Title = ballot.Name,
                    ElectionId = ballot.ElectionId,
                    BallotId = ballot.BallotId
                };
                return electionResponse;
            }
        }


        public async Task<Result<List<Entities.County>>> GetCounties()
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var counties = await dbContext.Counties.ToListAsync();
                return Result.Success(counties);
            }
        }

        public async Task<Result<List<Locality>>> GetLocalities()
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var localities = await dbContext.Localities.ToListAsync();
                return Result.Success(localities);
            }
        }

        public async Task<Result<List<Locality>>> GetCountries()
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var countries = await dbContext.Localities.Where(l => l.CountyId == 16820).ToListAsync();
                return Result.Success(countries);
            }
        }
    }
}
