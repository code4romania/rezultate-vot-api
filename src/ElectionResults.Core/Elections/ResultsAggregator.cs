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
using ElectionResults.Core.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static System.String;

namespace ElectionResults.Core.Elections
{
    public class ResultsAggregator : IResultsAggregator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ICsvDownloaderJob _csvDownloaderJob;
        private readonly IPartiesRepository _partiesRepository;
        private readonly IWinnersAggregator _winnersAggregator;
        private readonly IElectionsRepository _electionRepository;

        public ResultsAggregator(IServiceProvider serviceProvider,
            ICsvDownloaderJob csvDownloaderJob,
            IPartiesRepository partiesRepository,
            IWinnersAggregator winnersAggregator,
            IElectionsRepository electionRepository)
        {
            _serviceProvider = serviceProvider;
            _csvDownloaderJob = csvDownloaderJob;
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
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var ballot = dbContext.Ballots
                    .AsNoTracking()
                    .Include(b => b.Election)
                    .FirstOrDefault(e => e.BallotId == query.BallotId);
                if (ballot == null)
                    throw new Exception($"No results found for ballot id {query.BallotId}");
                var electionResponse = new ElectionResponse();

                var divisionTurnout =
                    await GetDivisionTurnout(query, dbContext, ballot);
                var electionInfo = await GetCandidatesFromDb(query, ballot, dbContext);
                if (electionInfo.TotalVotes > 0)
                {
                    divisionTurnout = new Turnout
                    {
                        EligibleVoters = electionInfo.EligibleVoters,
                        TotalVotes = electionInfo.TotalVotes,
                        ValidVotes = electionInfo.ValidVotes,
                        NullVotes = electionInfo.NullVotes
                    };
                }
                ElectionResultsResponse results;
                if (divisionTurnout == null)
                {
                    results = null;
                }
                else
                {
                    var parties = await _partiesRepository.GetAllParties();
                    results = ResultsProcessor.PopulateElectionResults(divisionTurnout, ballot, electionInfo.Candidates, parties.ToList());
                }

                electionResponse.Results = results;
                electionResponse.Observation = await dbContext.Observations.FirstOrDefaultAsync(o => o.BallotId == ballot.BallotId);
                if (divisionTurnout != null)
                {
                    electionResponse.Turnout = new ElectionTurnout
                    {
                        TotalVotes = divisionTurnout.TotalVotes,
                        EligibleVoters = divisionTurnout.EligibleVoters,
                    };
                }

                electionResponse.Scope = await CreateElectionScope(dbContext, query);
                electionResponse.Meta = CreateElectionMeta(ballot);
                electionResponse.ElectionNews = await GetElectionNews(dbContext, ballot.BallotId, ballot.ElectionId);
                return electionResponse;
            }
        }

        private async Task<ElectionScope> CreateElectionScope(ApplicationDbContext dbContext, ElectionResultsQuery query)
        {
            var electionScope = new ElectionScope
            {
                Type = query.Division
            };
            County county;
            if (query.Division == ElectionDivision.County)
            {
                county = await dbContext.Counties.FirstOrDefaultAsync(c => c.CountyId == query.CountyId);
                electionScope.CountyId = query.CountyId;
                electionScope.CountyName = county?.Name;
            }
            else
            {
                var locality = await dbContext.Localities.FirstOrDefaultAsync(c => c.LocalityId == query.LocalityId);
                if (query.Division == ElectionDivision.Locality)
                {
                    electionScope.LocalityId = query.LocalityId;
                    electionScope.LocalityName = locality?.Name;
                    electionScope.CountyId = locality?.CountyId;
                    county = await dbContext.Counties.FirstOrDefaultAsync(c => c.CountyId == query.CountyId);
                    electionScope.CountyName = county?.Name;
                }
                else if (query.Division == ElectionDivision.Diaspora_Country)
                {
                    var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == query.CountryId);
                    electionScope.CountryId = query.CountryId;
                    electionScope.CountryName = country?.Name;
                }
            }

            return electionScope;
        }

        private static async Task<List<ArticleResponse>> GetElectionNews(ApplicationDbContext dbContext, int ballotId, int electionId)
        {
            // var ballotNews = await dbContext.Articles.Where(a => a.BallotId == ballotId)
            //     .Include(a => a.Author)
            //     .Include(a => a.Pictures)
            //     .ToListAsync();
            // if (ballotNews == null || ballotNews.Any() == false)
            // {
            var ballotNews = await dbContext.Articles
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

        private async Task<Turnout> GetDivisionTurnout(ElectionResultsQuery query, ApplicationDbContext dbContext, Ballot ballot)
        {
            if (ballot.Election.Category == ElectionCategory.Local && ballot.DoesNotAllowDivision(query.Division) && !ballot.Election.Live)
            {
                return await RetrieveAggregatedTurnoutForCityHalls(query, ballot, dbContext);
            }

            return await dbContext.Turnouts
                .FirstOrDefaultAsync(t =>
                    t.BallotId == ballot.BallotId &&
                    t.CountyId == query.CountyId &&
                    t.CountryId == query.CountryId &&
                    t.LocalityId == query.LocalityId);
        }

        private static async Task<Turnout> RetrieveAggregatedTurnoutForCityHalls(ElectionResultsQuery query,
            Ballot ballot, ApplicationDbContext dbContext)
        {
            if (ballot.Election.Live && query.Division == ElectionDivision.National)
            {
                var nationalTurnout = await dbContext.Turnouts
                    .FirstOrDefaultAsync(t =>
                        t.BallotId == ballot.BallotId && t.Division == ElectionDivision.National);
                return nationalTurnout;

            }
            var turnout = new Turnout();
            var queryable = dbContext.Turnouts
                .Where(t =>
                    t.BallotId == ballot.BallotId &&
                    t.CountyId == query.CountyId &&
                    t.Division == ElectionDivision.Locality);

            if (ballot.Election.Live)
            {
                if (query.Division == ElectionDivision.County)
                {
                    if (ballot.BallotType != BallotType.Mayor && ballot.BallotType != BallotType.LocalCouncil)
                    {
                        queryable = dbContext.Turnouts
                            .Where(t =>
                                t.BallotId == ballot.BallotId &&
                                t.Division == ElectionDivision.County);
                    }
                }
            }

            var turnoutsForCounty = await queryable.ToListAsync();

            turnout.BallotId = ballot.BallotId;
            turnout.EligibleVoters = turnoutsForCounty.Sum(c => c.EligibleVoters);
            turnout.TotalVotes = turnoutsForCounty.Sum(c => c.TotalVotes);
            turnout.ValidVotes = turnoutsForCounty.Sum(c => c.ValidVotes);
            turnout.NullVotes = turnoutsForCounty.Sum(c => c.NullVotes);
            return turnout;
        }

        private async Task<LiveElectionInfo> GetCandidatesFromDb(ElectionResultsQuery query, Ballot ballot,
            ApplicationDbContext dbContext)
        {
            LiveElectionInfo liveElectionInfo = new LiveElectionInfo();
            if (ballot.Election.Live)
            {
                try
                {
                    var url = await GetFileUrl(query, dbContext, ballot);
                    if (url.IsEmpty())
                        return new LiveElectionInfo();
                    liveElectionInfo = await _csvDownloaderJob.GetCandidatesFromUrl(url);
                    var candidates = liveElectionInfo.Candidates;
                    var parties = await dbContext.Parties.ToListAsync();
                    var candidatesForThisElection = await GetCandidateResultsFromQueryAndBallot(query, ballot, dbContext);
                    var dbCandidates = new List<CandidateResult>();
                    if (candidates == null)
                    {
                        liveElectionInfo.Candidates = dbCandidates;
                        return liveElectionInfo;
                    }
                    foreach (var candidate in candidates)
                    {
                        dbCandidates.Add(PopulateCandidateData(candidatesForThisElection, candidate, parties, ballot));
                    }

                    liveElectionInfo.Candidates = dbCandidates;
                    return liveElectionInfo;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Probably there are no votes ");
                    Console.WriteLine(e);
                    return new LiveElectionInfo();
                }
            }
            if (ballot.Election.Category == ElectionCategory.Local && CountyIsNotBucharest(query))
            {
                if (ballot.DoesNotAllowDivision(query.Division) && !ballot.Election.Live)
                {
                    var aggregatedVotes = await RetrieveAggregatedVotes(query, ballot);
                    liveElectionInfo.Candidates = aggregatedVotes;
                    return liveElectionInfo;
                }
            }

            var results = await GetCandidateResultsFromQueryAndBallot(query, ballot, dbContext);
            liveElectionInfo.Candidates = results;
            return liveElectionInfo;
        }

        private static CandidateResult PopulateCandidateData(List<CandidateResult> candidatesForThisElection, CandidateResult candidate, List<Party> parties, Ballot ballot)
        {
            if (ballot.BallotType == BallotType.CountyCouncil || ballot.BallotType == BallotType.LocalCouncil)
            {
                var party = parties.Where(p => p.Alias != null).FirstOrDefault(p => candidate.Name.ContainsString(p.Alias));
                if (party != null)
                {
                    PopulatePartyInfo(candidate, party);
                }
                else
                {
                    candidate.PartyName = candidate.Name;
                }
                return candidate;
            }
            var dbCandidate =
                candidatesForThisElection.FirstOrDefault(c => candidate.Name.ContainsString(c.PartyName));
            if (dbCandidate != null)
            {
                dbCandidate.Votes = candidate.Votes;
            }
            else
            {
                var party = parties.Where(p => p.Alias != null).FirstOrDefault(p => candidate.Name.ContainsString(p.Alias));
                if (party == null)
                {
                    party = parties.Where(p => p.Name.Length > 10).FirstOrDefault(p => candidate.Name.ContainsString(p.Name));
                }
                if (party != null)
                {
                    PopulateCandidateWithPartyInfo(candidate, party);
                }
                else
                {
                    return candidate;
                }
            }
            if (dbCandidate == null)
                return candidate;
            return dbCandidate;
        }

        private static void PopulateCandidateWithPartyInfo(CandidateResult candidate, Party party)
        {
            candidate.Party = party;
            candidate.PartyId = party.Id;
            if (party?.Alias != null)
                candidate.Name = candidate.Name.Replace(party.Alias, "");
            if (party.Name != null)
                candidate.Name = candidate.Name.Replace(party.Name, "");
            candidate.Name = candidate.Name.Trim('-', '.', ' ');
        }
        private static void PopulatePartyInfo(CandidateResult candidate, Party party)
        {
            candidate.Party = party;
            candidate.PartyId = party.Id;
        }
        private static async Task<List<CandidateResult>> GetCandidateResultsFromQueryAndBallot(ElectionResultsQuery query, Ballot ballot,
            ApplicationDbContext dbContext)
        {
            var resultsQuery = dbContext.CandidateResults
                .Include(c => c.Party)
                .Where(er =>
                    er.BallotId == ballot.BallotId &&
                    er.Division == query.Division &&
                    er.CountyId == query.CountyId &&
                    er.CountryId == query.CountryId &&
                    er.LocalityId == query.LocalityId);
            return await resultsQuery.ToListAsync();
        }

        private async Task<string> GetFileUrl(ElectionResultsQuery query, ApplicationDbContext dbContext, Ballot ballot)
        {
            if (query.Division == ElectionDivision.Locality)
            {
                var locality = await dbContext.Localities
                    .Include(l => l.County)
                    .FirstOrDefaultAsync(l => l.LocalityId == query.LocalityId);
                if (locality == null)
                    return Empty;
                if (ballot.BallotType == BallotType.Mayor)
                {
                    return $@"https://prezenta.roaep.ro/locale27092020/data/csv/sicpv/pv_part_uat_p_{locality.County.ShortName.ToLower()}_{locality.Siruta}.csv";
                }

                if (ballot.BallotType == BallotType.LocalCouncil)
                {
                    return $@"https://prezenta.roaep.ro/locale27092020/data/csv/sicpv/pv_part_uat_cl_{locality.County.ShortName.ToLower()}_{locality.Siruta}.csv";
                }

            }

            if (query.Division == ElectionDivision.County)
            {
                var county = await dbContext.Counties
                    .FirstOrDefaultAsync(l => l.CountyId == query.CountyId);
                if (county == null)
                    return Empty;
                if (query.CountyId == 12913)
                {
                    if (ballot.BallotType == BallotType.Mayor || ballot.BallotType == BallotType.CountyCouncilPresident || ballot.BallotType == BallotType.CapitalCityMayor)
                        return "https://prezenta.roaep.ro/locale27092020/data/csv/sicpv/pv_part_cnty_pcj_b.csv";
                }
                switch (ballot.BallotType)
                {
                    case BallotType.CountyCouncil:
                        return $@"https://prezenta.roaep.ro/locale27092020/data/csv/sicpv/pv_part_cnty_cj_{county.ShortName.ToLower()}.csv";
                    case BallotType.CountyCouncilPresident:
                        return $@"https://prezenta.roaep.ro/locale27092020/data/csv/sicpv/pv_part_cnty_pcj_{county.ShortName.ToLower()}.csv";
                }
            }
            return Empty;
        }

        private static bool CountyIsNotBucharest(ElectionResultsQuery query)
        {
            return query.CountyId != 12913;
        }

        private async Task<List<CandidateResult>> RetrieveAggregatedVotes(ElectionResultsQuery query, Ballot ballot)
        {
            switch (query.Division)
            {
                case ElectionDivision.County:
                    {
                        var result = await _winnersAggregator.GetLocalityCityHallWinnersByCounty(ballot.BallotId, query.CountyId.GetValueOrDefault());
                        if (result.IsSuccess)
                        {
                            var candidateResults = _winnersAggregator.RetrieveFirst10Winners(result.Value.Select(w => w.Candidate).ToList(), ballot.BallotType);

                            return candidateResults;
                        }
                        throw new Exception(result.Error);
                    }
                case ElectionDivision.National:
                    {
                        var result = await _winnersAggregator.GetAllLocalityWinners(ballot.BallotId);
                        if (result.IsSuccess)
                        {
                            var candidateResults = _winnersAggregator.RetrieveFirst10Winners(result.Value, ballot.BallotType);
                            return candidateResults;
                        }
                        throw new Exception(result.Error);
                    }
                default:
                    throw new ArgumentOutOfRangeException(nameof(query));
            }
        }

        private static ElectionMeta CreateElectionMeta(Ballot ballot)
        {
            return new ElectionMeta
            {
                Date = ballot.Date,
                Type = ballot.BallotType,
                Ballot = ballot.Name,
                Subtitle = ballot.Election.Subtitle,
                Title = ballot.Election.Name,
                ElectionId = ballot.ElectionId,
                BallotId = ballot.BallotId,
                Live = ballot.Election.Live
            };
        }

        public async Task<List<ArticleResponse>> GetNewsFeed(ElectionResultsQuery query, int electionId)
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                return await GetElectionNews(dbContext, query.BallotId, electionId);
            }
        }
    }
}
