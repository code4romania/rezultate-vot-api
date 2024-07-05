using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Repositories;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Elections
{
    public class ResultsAggregator : IResultsAggregator
    {
        private readonly IPartiesRepository _partiesRepository;
        private readonly IWinnersAggregator _winnersAggregator;
        private readonly IElectionsRepository _electionRepository;
        private readonly ApplicationDbContext _dbContext;

        public ResultsAggregator(ApplicationDbContext dbContext,
            IPartiesRepository partiesRepository,
            IWinnersAggregator winnersAggregator,
            IElectionsRepository electionRepository)
        {
            _dbContext = dbContext;
            _partiesRepository = partiesRepository;
            _winnersAggregator = winnersAggregator;
            _electionRepository = electionRepository;
        }

        public async Task<Result<List<ElectionMeta>>> GetAllBallots()
        {
            var electionsResult = await _electionRepository.GetAllElections(true);
            if (electionsResult.IsFailure)
                return Result.Failure<List<ElectionMeta>>(electionsResult.Error);
            var elections = electionsResult.Value;
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
                             Live = election.Live,
                             BallotId = electionBallot.BallotId,
                             Round = electionBallot.Round == 0 ? null : electionBallot.Round
                         }).ToList();
            return Result.Success(metas);
        }

        public async Task<Result<ElectionResponse>> GetBallotResults(ElectionResultsQuery query)
        {
            var ballot = await _dbContext.Ballots
                .AsNoTracking()
                .Include(b => b.Election)
                .Where(e => e.BallotId == query.BallotId)
                .FirstOrDefaultAsync();

            if (QueryIsForCapitalCity(query, ballot))
            {
                BallotType ballotType = ballot.BallotType;
                if (ballot.BallotType == BallotType.Mayor)
                {
                    ballotType = BallotType.CountyCouncilPresident;
                }

                if (ballot.BallotType == BallotType.LocalCouncil)
                {
                    ballotType = BallotType.CountyCouncil;
                }

                ballot = await _dbContext.Ballots
                    .AsNoTracking()
                    .Include(b => b.Election)
                    .Where(e => e.ElectionId == ballot.ElectionId && e.BallotType == ballotType)
                    .FirstOrDefaultAsync();
            }

            if (ballot == null)
            {
                throw new Exception($"No results found for ballot id {query.BallotId}");
            }

            var electionResponse = new ElectionResponse();

            var divisionTurnout = await GetDivisionTurnout(query, ballot);
            var electionInfo = await GetCandidatesFromDb(query, ballot);

            if (electionInfo.TotalVotes > 0)
            {
                divisionTurnout = new Turnout
                {
                    EligibleVoters = divisionTurnout.EligibleVoters,
                    CountedVotes = electionInfo.ValidVotes + electionInfo.NullVotes,
                    TotalVotes = divisionTurnout.TotalVotes,
                    ValidVotes = electionInfo.ValidVotes,
                    NullVotes = electionInfo.NullVotes
                };
            }
            ElectionResultsResponse results;
            if (divisionTurnout == null)
            {
                results = new ElectionResultsResponse
                {
                    TotalVotes = 0,
                    EligibleVoters = 0,
                    NullVotes = 0,
                    ValidVotes = 0,
                    Candidates = new List<CandidateResponse>()
                };
            }
            else
            {
                var parties = await _partiesRepository.GetAllParties();
                results = ResultsProcessor.PopulateElectionResults(divisionTurnout, ballot, electionInfo.Candidates, parties.ToList());
            }

            electionResponse.Aggregated = electionInfo.Aggregated;
            electionResponse.Results = results;
            electionResponse.Observation = await _dbContext
                .Observations
                .Where(o => o.BallotId == ballot.BallotId)
                .FirstOrDefaultAsync();
            
            if (divisionTurnout != null)
            {
                electionResponse.Turnout = new ElectionTurnout
                {
                    TotalVotes = divisionTurnout.TotalVotes,
                    EligibleVoters = divisionTurnout.EligibleVoters,
                };
                if (query.Division == ElectionDivision.Diaspora ||
                    query.Division == ElectionDivision.Diaspora_Country)
                {
                    electionResponse.Turnout.EligibleVoters = electionResponse.Turnout.TotalVotes;
                }
            }

            electionResponse.Scope = await CreateElectionScope(query);
            electionResponse.Meta = CreateElectionMeta(query, ballot);
            electionResponse.ElectionNews = await GetElectionNews(ballot.BallotId, ballot.ElectionId);
            return electionResponse;
        }

        private static bool QueryIsForCapitalCity(ElectionResultsQuery query, Ballot ballot)
        {
            return query.CountyId != null
                   && query.CountyId.Value.IsCapitalCity()
                   && query.Division == ElectionDivision.County
                   && ballot.Date.Year >= 2020;
        }

        private async Task<ElectionScope> CreateElectionScope(ElectionResultsQuery query)
        {
            var electionScope = new ElectionScope
            {
                Type = query.Division
            };
            County county;
            if (query.Division == ElectionDivision.County)
            {
                county = await _dbContext.Counties
                    .Where(c => c.CountyId == query.CountyId)
                    .FirstOrDefaultAsync();
                electionScope.CountyId = query.CountyId;
                electionScope.CountyName = county?.Name;
            }
            else
            {
                var locality = await _dbContext.Localities
                    .Where(c => c.LocalityId == query.LocalityId)
                    .FirstOrDefaultAsync();
                
                if (query.Division == ElectionDivision.Locality)
                {
                    electionScope.LocalityId = query.LocalityId;
                    electionScope.LocalityName = locality?.Name;
                    electionScope.CountyId = locality?.CountyId;
                    county = await _dbContext.Counties.Where(c => c.CountyId == query.CountyId).FirstOrDefaultAsync();
                    electionScope.CountyName = county?.Name;
                }
                else if (query.Division == ElectionDivision.Diaspora_Country)
                {
                    var country = await _dbContext.Countries.Where(c => c.Id == query.CountryId).FirstOrDefaultAsync();
                    electionScope.CountryId = query.CountryId;
                    electionScope.CountryName = country?.Name;
                }
            }

            return electionScope;
        }

        private async Task<List<ArticleResponse>> GetElectionNews(int ballotId, int electionId)
        {
            // var ballotNews = await dbContext.Articles.Where(a => a.BallotId == ballotId)
            //     .Include(a => a.Author)
            //     .Include(a => a.Pictures)
            //     .ToListAsync();
            // if (ballotNews == null || ballotNews.Any() == false)
            // {
            var ballotNews = await _dbContext.Articles
                .Include(a => a.Author)
                .Include(a => a.Pictures)
                .Where(a => a.ElectionId == electionId).ToListAsync();
            //}

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
            })
            .OrderByDescending(e => e.Timestamp)
            .ToList();

            return electionNews;
        }

        public async Task<Turnout> GetDivisionTurnout(ElectionResultsQuery query, Ballot ballot)
        {
            if (ballot.Election.Category == ElectionCategory.Local && !ballot.AllowsDivision(query.Division, query.LocalityId.GetValueOrDefault()) && !ballot.Election.Live)
            {
                return await RetrieveAggregatedTurnoutForCityHalls(query, ballot);
            }

            var turnouts = await _dbContext.Turnouts
                .Where(t =>
                    t.BallotId == ballot.BallotId
                    && t.CountyId == query.CountyId
                    && t.CountryId == query.CountryId
                    && t.Division == query.Division
                    && t.LocalityId == query.LocalityId)
                .ToListAsync();

            if (turnouts.Count > 0 && query.Division == ElectionDivision.Diaspora_Country)
            {
                var turnout = AggregateTurnouts(ballot.BallotId, turnouts);
                return turnout;
            }

            if (turnouts.Count == 0 && query.Division == ElectionDivision.Diaspora)
            {
                turnouts = await _dbContext.Turnouts
                    .Where(t => t.BallotId == ballot.BallotId
                                && t.CountyId == null
                                && t.Division == ElectionDivision.Diaspora_Country
                                && t.LocalityId == null)
                    .ToListAsync();

                var turnout = AggregateTurnouts(ballot.BallotId, turnouts);
                return turnout;
            }

            var divisionTurnout = turnouts.FirstOrDefault();
            if (divisionTurnout != null)
            {
                divisionTurnout.CountedVotes = divisionTurnout.ValidVotes + divisionTurnout.NullVotes;
            }
            return divisionTurnout;
        }

        private async Task<Turnout> RetrieveAggregatedTurnoutForCityHalls(ElectionResultsQuery query, Ballot ballot)
        {
            IQueryable<Turnout> queryable = _dbContext.Turnouts
                .Where(t => t.BallotId == ballot.BallotId);

            if (ballot.BallotType == BallotType.CountyCouncilPresident || ballot.BallotType == BallotType.CountyCouncil)
            {
                queryable = queryable
                    .Where(t => t.Division == ElectionDivision.County);
            }
            else
            {
                queryable = queryable
                    .Where(t => t.Division == ElectionDivision.Locality);
            }

            if (query.CountyId != null)
            {
                queryable = queryable.Where(c => c.CountyId == query.CountyId);
            }

            var turnoutsForCounty = await queryable.ToListAsync();

            var turnout = AggregateTurnouts(ballot.BallotId, turnoutsForCounty);
            return turnout;
        }

        private static Turnout AggregateTurnouts(int ballotBallotId, List<Turnout> turnouts)
        {
            var totalVotes = turnouts.Sum(c => c.TotalVotes);
            var eligibleVoters = turnouts.Sum(c => c.EligibleVoters);
            var validVotes = turnouts.Sum(c => c.ValidVotes);
            var nullVotes = turnouts.Sum(c => c.NullVotes);

            Turnout turnout = Turnout.New(ballotBallotId, totalVotes, eligibleVoters, validVotes, nullVotes);

            return turnout;
        }

        private async Task<LiveElectionInfo> GetCandidatesFromDb(ElectionResultsQuery query, Ballot ballot)
        {
            LiveElectionInfo liveElectionInfo = new LiveElectionInfo();

            if (ballot.Election.Category == ElectionCategory.Local && query.CountyId.GetValueOrDefault().IsCapitalCity() == false)
            {
                if (!ballot.AllowsDivision(query.Division, query.LocalityId.GetValueOrDefault()) && !ballot.Election.Live)
                {
                    var aggregatedVotes = await RetrieveAggregatedVotes(query, ballot);
                    liveElectionInfo.Candidates = aggregatedVotes;
                    liveElectionInfo.Aggregated = true;

                    return liveElectionInfo;
                }
            }

            var results = await GetCandidateResultsFromQueryAndBallot(query, ballot);
            liveElectionInfo.Candidates = results;
            return liveElectionInfo;
        }

        private async Task<List<CandidateResult>> GetCandidateResultsFromQueryAndBallot(ElectionResultsQuery query, Ballot ballot)
        {
            var resultsQuery = _dbContext.CandidateResults
                .Include(c => c.Party)
                .Where(er =>
                    er.BallotId == ballot.BallotId &&
                    er.Division == query.Division &&
                    er.CountyId == query.CountyId &&
                    er.CountryId == query.CountryId &&
                    er.LocalityId == query.LocalityId);

            var results = await resultsQuery.ToListAsync();

            if (query.Division == ElectionDivision.Diaspora_Country || query.Division == ElectionDivision.Diaspora)
            {
                results = results.GroupBy(r => r.Name).Select(r =>
                {
                    var candidate = r.FirstOrDefault();
                    candidate.Votes = r.Sum(c => c.Votes);
                    return candidate;
                }).ToList();
            }

            return results;
        }

        private async Task<List<CandidateResult>> RetrieveAggregatedVotes(ElectionResultsQuery query, Ballot ballot)
        {
            switch (query.Division)
            {
                case ElectionDivision.County:
                    {
                        var takeOnlyWinner = ballot.BallotType != BallotType.LocalCouncil;
                        var result = await _winnersAggregator.GetLocalityCityHallWinnersByCounty(ballot.BallotId, query.CountyId.GetValueOrDefault(), takeOnlyWinner);
                        if (result.IsSuccess)
                        {
                            var candidateResults = _winnersAggregator.RetrieveWinners(result.Value.Select(w => w.Candidate).ToList(), ballot.BallotType);

                            return candidateResults;
                        }
                        throw new Exception(result.Error);
                    }
                case ElectionDivision.National:
                    {
                        Result<List<CandidateResult>> result;
                        if (ballot.BallotType == BallotType.CountyCouncil ||
                            ballot.BallotType == BallotType.CountyCouncilPresident)
                        {
                            result = await _winnersAggregator.GetWinningCandidatesByCounty(ballot.BallotId);
                            if (result.IsSuccess)
                                return _winnersAggregator.RetrieveWinners(result.Value, ballot.BallotType);
                        }
                        else
                        {
                            var resultsForElection = await _dbContext.CandidateResults
                                .Include(c => c.Party)
                                .Where(c => c.BallotId == query.BallotId && c.Division == ElectionDivision.Locality)
                                .ToListAsync();
                            return _winnersAggregator.RetrieveWinners(resultsForElection, ballot.BallotType);
                        }
                        throw new Exception(result.Error);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(query));
            }
        }

        private ElectionMeta CreateElectionMeta(ElectionResultsQuery query, Ballot ballot)
        {
            var electionMeta = new ElectionMeta
            {
                Date = ballot.Date,
                Type = ballot.BallotType,
                Ballot = ballot.Name,
                Subtitle = ballot.Election.Subtitle,
                Title = ballot.Election.Name,
                ElectionId = ballot.ElectionId,
                BallotId = ballot.BallotId,
                Live = ballot.Election.Live,
                Stage = ballot.Election.Live ? "prov" : "final"
            };
            if (QueryIsForCapitalCity(query, ballot))
            {
                electionMeta.Ballot = "Primăria Capitalei";
            }
            return electionMeta;
        }

        public async Task<List<ArticleResponse>> GetNewsFeed(ElectionResultsQuery query, int electionId)
        {
            return await GetElectionNews(query.BallotId, electionId);
        }

        public async Task<Result<List<PartyList>>> GetBallotCandidates(ElectionResultsQuery query)
        {
            var ballot = await _dbContext.Ballots
                .Include(b => b.Election)
                .AsNoTracking()
                .Where(e => e.BallotId == query.BallotId)
                .FirstOrDefaultAsync();

            var candidates = await GetCandidateResultsFromQueryAndBallot(query, ballot);
            var minorities = await GetMinorities(ballot);
            candidates = candidates.Concat(minorities).ToList();
            return candidates
                .GroupBy(c => c.PartyName)
                .Select(p => new PartyList
                {
                    Candidates = p.OrderBy(c => c.BallotPosition).Select(c => new BasicCandidateInfo
                    {
                        Name = c.Name
                    }).ToList(),
                    Name = p.FirstOrDefault()?.PartyName
                }).ToList();
        }

        private async Task<List<CandidateResult>> GetMinorities(Ballot ballot)
        {
            var query = new ElectionResultsQuery();
            query.CountyId = null;
            query.Division = ElectionDivision.County;
            var minorities = await GetCandidateResultsFromQueryAndBallot(query, ballot);
            return minorities;
        }
    }

    internal class LiveElectionInfo
    {
        public int TotalVotes { get; set; }
        public int ValidVotes { get; set; }
        public int NullVotes { get; set; }
        public bool Aggregated { get; set; }
        public List<CandidateResult> Candidates { get; set; }
    }
}
