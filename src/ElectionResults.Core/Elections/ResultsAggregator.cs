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
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionResults.Core.Elections
{
    public class ResultsAggregator : IResultsAggregator
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IAppCache _appCache;
        private string _partiesKey = "parties";
        private string _countriesKey = "countries";
        private string _countiesKey = "counties";

        public ResultsAggregator(IServiceProvider serviceProvider, IAppCache appCache)
        {
            _serviceProvider = serviceProvider;
            _appCache = appCache;
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
                var electionResponse = new ElectionResponse();

                var candidates = await GetCandidatesFromDb(query, ballot, dbContext);
                var divisionTurnout = await GetDivisionTurnout(query, dbContext, ballot);
                var electionTurnout = await GetElectionTurnout(dbContext, ballot);

                ElectionResultsResponse results;
                if (divisionTurnout == null)
                {
                    results = null;
                }
                else
                {
                    var parties = await _appCache.GetOrAddAsync(
                        _partiesKey, () => dbContext.Parties.ToListAsync(),
                        DateTimeOffset.Now.AddMinutes(5));
                    results = ProcessResults(divisionTurnout, ballot, candidates, parties);
                }

                electionResponse.Results = results;
                electionResponse.Observation = await dbContext.Observations.FirstOrDefaultAsync(o => o.BallotId == ballot.BallotId);
                if (electionTurnout != null)
                {
                    electionResponse.Turnout = new ElectionTurnout
                    {
                        TotalVotes = electionTurnout.TotalVotes,
                        EligibleVoters = electionTurnout.EligibleVoters,
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
                }

                electionResponse.Scope = await CreateElectionScope(dbContext, query);
                electionResponse.Meta = CreateElectionMeta(ballot);
                electionResponse.ElectionNews = await GetElectionNews(dbContext, ballot);
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

        private ElectionResultsResponse ProcessResults(Turnout electionTurnout, Ballot ballot, List<CandidateResult> candidates, List<Party> parties)
        {
            ElectionResultsResponse results = new ElectionResultsResponse();
            results.NullVotes = electionTurnout.NullVotes;
            results.TotalVotes = electionTurnout.TotalVotes;
            results.ValidVotes = electionTurnout.ValidVotes;
            results.EligibleVoters = electionTurnout.EligibleVoters;
            results.TotalSeats = electionTurnout.TotalSeats;
            results.VotesByMail = electionTurnout.VotesByMail != 0 ? electionTurnout.VotesByMail : (int?)null;
            if (ballot.BallotType == BallotType.Referendum)
            {
                if (results.ValidVotes == 0)
                {
                    results.ValidVotes = results.TotalVotes - results.ValidVotes;
                }

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
                var colors = new List<string>();
                foreach (var candidate in candidates)
                {

                    var matchingParty = parties.FirstOrDefault(p =>
                        candidate.ShortName.ContainsString(p.ShortName + "-")
                        || candidate.ShortName.ContainsString(p.ShortName + "+")
                        || candidate.ShortName.ContainsString(p.ShortName + " +")
                        || candidate.ShortName.ContainsString("+" + p.ShortName)
                        || candidate.ShortName.ContainsString("+ " + p.ShortName)
                        || candidate.ShortName.ContainsString(" " + p.ShortName)
                        || candidate.ShortName.ContainsString(p.ShortName + " "));
                    if (matchingParty != null)
                    {
                        colors.Add(matchingParty.Color);
                    }
                    else colors.Add(null);
                }
                results.Candidates = candidates.Select(c => new CandidateResponse
                {
                    ShortName = GetCandidateShortName(c, ballot),
                    Name = GetCandidateName(c, ballot),
                    Votes = c.Votes,
                    PartyColor = GetPartyColor(c),
                    PartyLogo = c.Party?.LogoUrl,
                    Seats = c.TotalSeats,
                    SeatsGained = c.SeatsGained
                }).ToList();
                for (var i = 0; i < results.Candidates.Count; i++)
                {
                    var candidate = results.Candidates[i];
                    if (candidate.PartyColor.IsEmpty())
                    {
                        candidate.PartyColor = colors[i];
                    }
                }
            }

            results.Candidates = results.Candidates.OrderByDescending(c => c.Votes).ToList();
            return results;
        }

        private string GetCandidateShortName(CandidateResult c, Ballot ballot)
        {
            try
            {
                if (ballot.BallotType == BallotType.EuropeanParliament || ballot.BallotType == BallotType.Senate ||
                    ballot.BallotType == BallotType.House)
                    return c.ShortName;
                if (c.Name.IsParty() || c.Name.IsEmpty())
                    return c.ShortName;
                var processedName = ParseShortName(c.Name);
                return processedName;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                return c.ShortName;
            }
        }

        private string ParseShortName(string shortName)
        {
            var fullName = shortName.Replace("CANDIDAT INDEPENDENT - ", "");
            var firstInitial = fullName[0].ToString().ToUpper() + ". ";
            string firstname = string.Empty;
            if (fullName.Contains("-"))
            {
                firstname = fullName.Split("-")[0] + "-";
            }
            else firstname = fullName.Split(" ")[0] + " ";

            var candidateName = firstInitial + fullName.Replace(firstname, "");
            return candidateName;
        }

        private static string GetPartyColor(CandidateResult c)
        {
            if (c.Party != null && c.Party.Name.ToLower() == "independent")
                return null;
            return c.Party?.Color;
        }

        private static string GetCandidateName(CandidateResult c, Ballot ballot)
        {
            if (ballot.BallotType == BallotType.EuropeanParliament || ballot.BallotType == BallotType.Senate ||
                ballot.BallotType == BallotType.House)
                return c.Party?.Name.Or(c.PartyName).Or(c.Name) ?? c.Name.Or(c.PartyName);
            return c.Name;
        }

        private static async Task<List<ArticleResponse>> GetElectionNews(ApplicationDbContext dbContext, Ballot ballot)
        {
            var ballotNews = await dbContext.Articles.Where(a => a.BallotId == ballot.BallotId)
                .Include(a => a.Author)
                .Include(a => a.Pictures)
                .ToListAsync();
            if (ballotNews == null || ballotNews.Any() == false)
            {
                ballotNews = await dbContext.Articles
                    .Include(a => a.Author)
                    .Include(a => a.Pictures)
                    .Where(a => a.ElectionId == ballot.ElectionId).ToListAsync();
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

        private static async Task<Turnout> GetElectionTurnout(ApplicationDbContext dbContext, Ballot ballot)
        {
            return await dbContext.Turnouts
                .FirstOrDefaultAsync(t =>
                    t.BallotId == ballot.BallotId &&
                    t.Division == ElectionDivision.National);
        }

        private static async Task<Turnout> GetDivisionTurnout(ElectionResultsQuery query, ApplicationDbContext dbContext, Ballot ballot)
        {
            return await dbContext.Turnouts
                .FirstOrDefaultAsync(t =>
                    t.BallotId == ballot.BallotId &&
                    t.CountyId == query.CountyId &&
                    t.CountryId == query.CountryId &&
                    t.LocalityId == query.LocalityId);
        }

        private async Task<List<CandidateResult>> GetCandidatesFromDb(ElectionResultsQuery query, Ballot ballot,
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
                BallotId = ballot.BallotId
            };
        }


        public async Task<Result<List<County>>> GetCounties()
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var counties = await dbContext.Counties.OrderBy(c => c.Name).ToListAsync();
                return Result.Success(counties);
            }
        }

        public async Task<Result<List<Locality>>> GetLocalities(int? countyId)
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                IQueryable<Locality> dbSet = dbContext.Localities;
                if (countyId.HasValue)
                    dbSet = dbSet.Where(l => l.CountyId == countyId.Value);
                var localities = await dbSet.OrderBy(l => l.Name).ToListAsync();
                return Result.Success(localities);
            }
        }

        public async Task<Result<List<Country>>> GetCountries()
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var countries = await dbContext.Countries.OrderBy(c => c.Name).ToListAsync();
                return Result.Success(countries);
            }
        }

        public async Task<Result<List<ElectionMapWinner>>> GetCountyWinners(int ballotId)
        {
            var winners = new List<ElectionMapWinner>();
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {

                var counties = await _appCache.GetOrAddAsync(
                    _countiesKey, () => dbContext.Counties.ToListAsync(),
                    DateTimeOffset.Now.AddMinutes(60));
                var ballot = await dbContext.Ballots.FirstOrDefaultAsync(b => b.BallotId == ballotId);
                foreach (var county in counties)
                {
                    var countyWinner = await dbContext.CandidateResults
                        .Include(b => b.Party)
                        .Where(c => c.BallotId == ballotId && c.CountyId == county.CountyId && c.Division == ElectionDivision.County)
                        .OrderByDescending(c => c.Votes)
                        .FirstOrDefaultAsync();
                    var turnoutForCountry = await dbContext.Turnouts
                        .Where(c => c.BallotId == ballotId && c.CountyId == county.CountyId && c.Division == ElectionDivision.County)
                        .FirstOrDefaultAsync();
                    if (countyWinner == null || turnoutForCountry == null)
                        continue;
                    var electionMapWinner = CreateElectionMapWinner(county.CountyId, ballot, countyWinner, turnoutForCountry);
                    winners.Add(electionMapWinner);
                }
            }

            return Result.Success(winners);
        }

        public async Task<Result<List<ElectionMapWinner>>> GetLocalityWinners(int ballotId, int countyId)
        {
            var winners = new List<ElectionMapWinner>();
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var localities = await dbContext.Localities.Where(l => l.CountyId == countyId).ToListAsync();
                var ballot = await dbContext.Ballots.FirstOrDefaultAsync(b => b.BallotId == ballotId);
                foreach (var locality in localities)
                {
                    var localityWinner = await dbContext.CandidateResults
                        .Include(b => b.Party)
                        .Where(c => c.BallotId == ballotId &&
                                    c.LocalityId == locality.LocalityId &&
                                    c.Division == ElectionDivision.Locality)
                        .OrderByDescending(c => c.Votes)
                        .FirstOrDefaultAsync();
                    var turnoutForCountry = await dbContext.Turnouts
                        .Where(c => c.BallotId == ballotId &&
                                    c.LocalityId == locality.LocalityId &&
                                    c.Division == ElectionDivision.Locality)
                        .FirstOrDefaultAsync();
                    if (localityWinner == null || turnoutForCountry == null)
                        continue;
                    var electionMapWinner = CreateElectionMapWinner(locality.LocalityId, ballot, localityWinner, turnoutForCountry);
                    winners.Add(electionMapWinner);
                }
            }

            return Result.Success(winners);
        }

        public async Task<Result<List<ElectionMapWinner>>> GetCountryWinners(int ballotId)
        {
            var winners = new List<ElectionMapWinner>();
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var countries = await _appCache.GetOrAddAsync(
                    _countriesKey, () => dbContext.Countries.ToListAsync(),
                    DateTimeOffset.Now.AddMinutes(60));
                var ballot = await dbContext.Ballots.FirstOrDefaultAsync(b => b.BallotId == ballotId);
                foreach (var country in countries)
                {
                    var countryWinner = await dbContext.CandidateResults
                        .Include(b => b.Party)
                        .Where(c => c.BallotId == ballotId && c.CountryId == country.Id && c.Division == ElectionDivision.Diaspora_Country)
                        .OrderByDescending(c => c.Votes)
                        .FirstOrDefaultAsync();
                    var turnoutForCountry = await dbContext.Turnouts
                        .Where(c => c.BallotId == ballotId && c.CountryId == country.Id && c.Division == ElectionDivision.Diaspora_Country)
                        .FirstOrDefaultAsync();
                    if (countryWinner == null || turnoutForCountry == null)
                        continue;

                    var electionMapWinner = CreateElectionMapWinner(country.Id, ballot, countryWinner, turnoutForCountry);

                    winners.Add(electionMapWinner);
                }
            }

            return Result.Success(winners);
        }

        private static ElectionMapWinner CreateElectionMapWinner(int id, Ballot ballot, CandidateResult winner,
            Turnout turnoutForCountry)
        {
            var electionMapWinner = new ElectionMapWinner
            {
                Id = id,
                Winner = new Winner()
            };
            if (ballot.BallotType != BallotType.Referendum)
            {
                electionMapWinner.Winner.Name = winner.Name;
                electionMapWinner.Winner.ShortName = winner.ShortName;
                electionMapWinner.Winner.Votes = winner.Votes;
                electionMapWinner.Winner.PartyColor = winner.Party?.Color;
            }
            else
            {
                if (winner.YesVotes > winner.NoVotes)
                {
                    electionMapWinner.Winner.Name = "DA";
                    electionMapWinner.Winner.ShortName = "DA";
                    electionMapWinner.Winner.Votes = winner.YesVotes;
                }
                else
                {
                    electionMapWinner.Winner.Name = "NU";
                    electionMapWinner.Winner.ShortName = "NU";
                    electionMapWinner.Winner.Votes = winner.NoVotes;
                }
            }

            electionMapWinner.ValidVotes = turnoutForCountry.ValidVotes;
            return electionMapWinner;
        }
    }
}
