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
                                 Title = election.Name ?? election.Subtitle ?? electionBallot.Name,
                                 Ballot = electionBallot.Name ?? election.Subtitle ?? electionBallot.Name,
                                 ElectionId = election.ElectionId,
                                 Type = electionBallot.BallotType,
                                 Subtitle = election.Subtitle,
                                 BallotId = electionBallot.BallotId,
                                 Round = electionBallot.Round == 0 ? null : electionBallot.Round
                             }).ToList();
                return Result.Success(metas);
            }
        }

        public async Task<Result<ElectionResponse>> GetBallotResults(ElectionResultsQuery query)
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var ballot = dbContext.Ballots
                    .AsNoTracking()
                    .Include(b => b.Election)
                    .FirstOrDefault(e => e.BallotId == query.BallotId);
                if (ballot == null)
                    throw new Exception($"No results found for ballot id {query.BallotId}");

                var results = new ElectionResultsResponse();

                var candidates = await GetCandidates(query, ballot, dbContext);
                var divisionTurnout = await GetDivisionTurnout(query, dbContext, ballot);
                var electionTurnout = await GetElectionTurnout(query, dbContext, ballot);
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
                        PartyColor = c.Party?.Color,
                        PartyLogo = c.Party?.LogoUrl
                    }).OrderByDescending(c => c.Votes).ToList();
                }

                var electionResponse = new ElectionResponse();
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
                electionResponse.Meta = CreateElectionMeta(ballot);
                electionResponse.ElectionNews = await GetElectionNews(dbContext, ballot);
                return electionResponse;
            }
        }

        private static async Task<List<ArticleResponse>> GetElectionNews(ApplicationDbContext dbContext, Ballot ballot)
        {
            var ballotNews = await dbContext.Articles.Where(a => a.BallotId == ballot.BallotId)
                .Include(a => a.Author)
                .Include(a => a.Pictures)
                .ToListAsync();
            if (ballotNews == null || ballotNews.Any() == false)
            {
                ballotNews = await dbContext.Articles.Where(a => a.ElectionId == ballot.ElectionId).ToListAsync();
            }

            var electionNews = ballotNews.Select(b => new ArticleResponse
            {
                Images = b.Pictures,
                Author = b.Author,
                Body = b.Body,
                Embed = b.Embed,
                Link = b.Link,
                Timestamp = b.Timestamp,
                Id = b.Id,
                Title = b.Title,
            }).ToList();
            return electionNews;
        }

        private static async Task<Turnout> GetElectionTurnout(ElectionResultsQuery query, ApplicationDbContext dbContext, Ballot ballot)
        {
            return await dbContext.Turnouts
                .FirstOrDefaultAsync(t =>
                    t.BallotId == ballot.BallotId && t.CountyId == query.CountyId && t.LocalityId == query.LocalityId);
        }

        private static async Task<Turnout> GetDivisionTurnout(ElectionResultsQuery query, ApplicationDbContext dbContext, Ballot ballot)
        {
            return await dbContext.Turnouts
                .FirstOrDefaultAsync(t =>
                    t.BallotId == ballot.BallotId &&
                    t.CountyId == query.CountyId &&
                    t.LocalityId == query.LocalityId);
        }

        private async Task<List<CandidateResult>> GetCandidates(ElectionResultsQuery query, Ballot ballot,
            ApplicationDbContext dbContext)
        {
            var resultsQuery = dbContext.CandidateResults
                .Include(c => c.Party)
                .Where(er =>
                er.BallotId == ballot.BallotId &&
                er.Division == query.Division &&
                er.CountyId == query.CountyId &&
                er.LocalityId == query.LocalityId);
            return await resultsQuery.ToListAsync();
        }

        private static ElectionMeta CreateElectionMeta(Ballot ballot)
        {
            return new ElectionMeta
            {
                Date = ballot.Date,
                Type = ballot.BallotType,
                Ballot = ballot.Name,
                Title = ballot.Election.Name,
                ElectionId = ballot.ElectionId,
                BallotId = ballot.BallotId
            };
        }


        public async Task<Result<List<County>>> GetCounties()
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
