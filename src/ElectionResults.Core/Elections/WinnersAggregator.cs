using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;

namespace ElectionResults.Core.Elections
{
    public class WinnersAggregator : IWinnersAggregator
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAppCache _appCache;
        private readonly IPartiesRepository _partiesRepository;
        private readonly ITerritoryRepository _territoryRepository;

        public WinnersAggregator(ApplicationDbContext dbContext, IAppCache appCache, IPartiesRepository partiesRepository, ITerritoryRepository territoryRepository)
        {
            _dbContext = dbContext;
            _appCache = appCache;
            _partiesRepository = partiesRepository;
            _territoryRepository = territoryRepository;
        }

        public async Task<Result<List<Winner>>> GetLocalityCityHallWinnersByCounty(int ballotId, int countyId, bool takeOnlyWinner = true)
        {
            var dbWinners = await GetWinners(ballotId, countyId, ElectionDivision.Locality);
            if (dbWinners.Count > 0)
                return dbWinners;
            _appCache.Remove(MemoryCache.CreateWinnersKey(ballotId, countyId, ElectionDivision.Locality));
            var localities = await _dbContext.Localities.Where(l => l.CountyId == countyId).ToListAsync();

            var candidateResultsForCounty = await _dbContext.CandidateResults
                .Include(c => c.Ballot)
                .Include(c => c.Party)
                .Where(c => c.BallotId == ballotId && c.Division == ElectionDivision.Locality).ToListAsync();
            var turnouts = await _dbContext.Turnouts
                .Where(c => c.BallotId == ballotId &&
                            c.Division == ElectionDivision.Locality)
                .ToListAsync();
            List<Winner> winningCandidates = new List<Winner>();
            foreach (var locality in localities)
            {
                var results = candidateResultsForCounty
                    .Where(c => c.LocalityId == locality.LocalityId)
                    .OrderByDescending(c => c.Votes).ToList();
                var localityWinner = results
                    .FirstOrDefault();
                var turnoutForLocality = turnouts
                    .FirstOrDefault(c => c.LocalityId == locality.LocalityId);
                if (localityWinner != null)
                {
                    if (takeOnlyWinner)
                    {
                        winningCandidates.Add(CreateWinner(ballotId, countyId, localityWinner, turnoutForLocality, ElectionDivision.Locality));
                    }
                    else
                    {
                        foreach (var candidateResult in results)
                        {
                            winningCandidates.Add(CreateWinner(ballotId, countyId, candidateResult, turnoutForLocality, ElectionDivision.Locality));
                        }
                    }
                }
            }

            await SaveWinners(winningCandidates);
            return Result.Success(winningCandidates);
        }

        private static Winner CreateWinner(int ballotId, int? countyId, CandidateResult localityWinner, Turnout turnoutForLocality, ElectionDivision division)
        {
            var winner = new Winner
            {
                BallotId = ballotId,
                CandidateId = localityWinner.Id,
                CountyId = countyId,
                Division = division,
                Name = localityWinner.Name,
                PartyId = localityWinner.PartyId,
                TurnoutId = turnoutForLocality?.Id,
            };
            return winner;
        }

        private async Task<List<Winner>> GetWinners(int ballotId, int? countyId, ElectionDivision division)
        {
            var query = CreateWinnersQuery()
                .Where(w => w.BallotId == ballotId
                            && w.Division == division
                            && w.CountyId == countyId).ToListAsync();
            var winnersKey = MemoryCache.CreateWinnersKey(ballotId, countyId, division);
            var winners = await _appCache.GetOrAddAsync(winnersKey,
                () => query, DateTimeOffset.Now.AddMinutes(10));
            return winners;
        }

        public async Task<Result<List<ElectionMapWinner>>> GetLocalityWinnersByCounty(int ballotId, int countyId)
        {
            var parties = await _partiesRepository.GetAllParties();
            var winners = await GetLocalityCityHallWinnersByCounty(ballotId, countyId);
            if (winners.IsSuccess)
                return winners.Value.Select(w => WinnerToElectionMapWinner(w, parties)).ToList();
            return Result.Failure<List<ElectionMapWinner>>(winners.Error);
        }

        private IIncludableQueryable<Winner, Party> CreateWinnersQuery()
        {
            return _dbContext.Winners
                .Include(w => w.Candidate.Party)
                .Include(w => w.Turnout)
                .Include(w => w.Ballot)
                .Include(w => w.Party);
        }

        private static ElectionMapWinner WinnerToElectionMapWinner(Winner winner, IEnumerable<Party> parties)
        {
            var electionMapWinner = CreateElectionMapWinner(winner.Candidate.LocalityId ?? winner.CountyId ?? winner.LocalityId ?? winner.CountryId, winner.Ballot, winner.Candidate, winner.Turnout);
            if (electionMapWinner.Winner.PartyColor.IsEmpty())
                electionMapWinner.Winner.PartyColor = parties.ToList().GetMatchingParty(winner.Candidate.ShortName)?.Color ?? Consts.IndependentCandidateColor;
            return electionMapWinner;
        }

        public async Task<Result<List<ElectionMapWinner>>> GetCountryWinners(int ballotId)
        {
            var parties = await _partiesRepository.GetAllParties();
            var dbWinners = await GetWinners(ballotId, null, ElectionDivision.Diaspora_Country);

            if (dbWinners.Count > 0)
                return dbWinners.Select(winner => WinnerToElectionMapWinner(winner, parties.ToList())).ToList();
            _appCache.Remove(MemoryCache.CreateWinnersKey(ballotId, null, ElectionDivision.Diaspora_Country));
            var winners = new List<ElectionMapWinner>();
            var countries = await _territoryRepository.GetCountries();
            if (countries.IsFailure)
                return Result.Failure<List<ElectionMapWinner>>(countries.Error);
            var ballot = await _dbContext.Ballots
                .Include(b => b.Election)
                .FirstOrDefaultAsync(b => b.BallotId == ballotId);

            var candidateResultsByCountries = await _dbContext.CandidateResults
                .Include(c => c.Party)
                .Where(c => c.BallotId == ballotId
                            && c.Division == ElectionDivision.Diaspora_Country)
                .ToListAsync();

            var turnouts = await _dbContext.Turnouts
                .Where(c => c.BallotId == ballotId &&
                            c.Division == ElectionDivision.Diaspora_Country)
                .ToListAsync();

            List<Winner> winningCandidates = new List<Winner>();
            foreach (var country in countries.Value)
            {
                var countryWinner = candidateResultsByCountries
                    .Where(c => c.CountryId == country.Id)
                    .OrderByDescending(c => c.Votes)
                    .FirstOrDefault();
                var turnoutForCountry = turnouts
                    .FirstOrDefault(c => c.CountryId == country.Id);
                if (countryWinner == null || turnoutForCountry == null)
                    continue;

                var electionMapWinner = CreateElectionMapWinner(country.Id, ballot, countryWinner, turnoutForCountry);
                if (electionMapWinner.Winner.PartyColor.IsEmpty())
                    electionMapWinner.Winner.PartyColor = parties.ToList().GetMatchingParty(countryWinner.ShortName)?.Color ?? Consts.IndependentCandidateColor;
                winners.Add(electionMapWinner);
                winningCandidates.Add(CreateWinner(ballot, countryWinner, electionMapWinner, turnoutForCountry.Id, country.Id, ElectionDivision.Diaspora_Country));
            }

            await SaveWinners(winningCandidates);

            return Result.Success(winners);
        }

        public async Task<Result<List<ElectionMapWinner>>> GetCountyWinners(int ballotId)
        {
            var parties = await _partiesRepository.GetAllParties();
            var dbWinners = await GetWinners(ballotId, null, ElectionDivision.County);
            if (dbWinners.Count > 0)
                return dbWinners.Select(winner => WinnerToElectionMapWinner(winner, parties)).ToList();
            _appCache.Remove(MemoryCache.CreateWinnersKey(ballotId, null, ElectionDivision.Diaspora_Country));
            var winners = await AggregateCountyWinners(ballotId, parties);
            return Result.Success(winners.Select(winner => WinnerToElectionMapWinner(winner, parties)).ToList());
        }

        private async Task<List<Winner>> AggregateCountyWinners(int ballotId, IEnumerable<Party> parties)
        {
            var counties = await _territoryRepository.GetCounties();
            var ballot = await _dbContext.Ballots
                .Include(b => b.Election)
                .FirstOrDefaultAsync(b => b.BallotId == ballotId);
            var candidateResultsByCounties = await _dbContext.CandidateResults
                .Include(c => c.Party)
                .Where(c => c.BallotId == ballotId
                            && c.Division == ElectionDivision.County)
                .ToListAsync();

            var turnouts = await _dbContext.Turnouts
                .Where(c => c.BallotId == ballotId &&
                            c.Division == ElectionDivision.County)
                .ToListAsync();
            var winningCandidates = new List<Winner>();
            foreach (var county in counties.Value)
            {
                var countyWinner = candidateResultsByCounties
                    .Where(c => c.CountyId == county.CountyId)
                    .OrderByDescending(c => c.Votes)
                    .FirstOrDefault();

                var turnoutForCounty = turnouts
                    .FirstOrDefault(c => c.CountyId == county.CountyId);

                if (countyWinner == null || turnoutForCounty == null)
                    continue;
                var electionMapWinner = CreateElectionMapWinner(county.CountyId, ballot, countyWinner, turnoutForCounty);
                if (electionMapWinner.Winner.PartyColor.IsEmpty())
                    electionMapWinner.Winner.PartyColor = parties.ToList().GetMatchingParty(countyWinner.ShortName)?.Color ?? Consts.IndependentCandidateColor;
                winningCandidates.Add(CreateWinner(ballot, countyWinner, electionMapWinner, turnoutForCounty.Id,
                    county.CountyId, ElectionDivision.County));
            }

            await SaveWinners(winningCandidates);
            return winningCandidates;
        }

        public async Task<Result<List<CandidateResult>>> GetWinningCandidatesByCounty(int ballotId)
        {
            var parties = await _partiesRepository.GetAllParties();
            var dbWinners = await GetWinners(ballotId, null, ElectionDivision.County);
            if (dbWinners.Count > 0)
                return dbWinners.Select(w => w.Candidate).ToList();
            _appCache.Remove(MemoryCache.CreateWinnersKey(ballotId, null, ElectionDivision.Diaspora_Country));
            var winners = await AggregateCountyWinners(ballotId, parties);
            return winners.Select(w => w.Candidate).ToList();
        }

        private static Winner CreateWinner(Ballot ballot, CandidateResult countyWinner,
            ElectionMapWinner electionMapWinner, int turnoutId, int countryId, ElectionDivision division)
        {
            return new Winner
            {
                BallotId = ballot.BallotId,
                Ballot = ballot,
                CandidateId = countyWinner.Id,
                Name = electionMapWinner.Winner.Name,
                PartyId = electionMapWinner.Winner.Party?.Id,
                Votes = electionMapWinner.Winner.Votes,
                TurnoutId = turnoutId,
                CountryId = countryId,
                Division = division
            };
        }

        private async Task SaveWinners(List<Winner> winningCandidates)
        {
            _dbContext.Winners.AddRange(winningCandidates);
            await _dbContext.SaveChangesAsync();
        }

        private static ElectionMapWinner CreateElectionMapWinner(int? id, Ballot ballot, CandidateResult winner,
            Turnout turnoutForCountry)
        {
            var electionMapWinner = new ElectionMapWinner
            {
                Id = id.GetValueOrDefault(),
                Winner = new MapWinner()
            };
            if (ballot.BallotType != BallotType.Referendum)
            {
                electionMapWinner.Winner.Name = winner.Name;
                electionMapWinner.Winner.ShortName = winner.ShortName;
                electionMapWinner.Winner.Votes = winner.Votes;
                electionMapWinner.Winner.PartyColor = winner.Party?.Color ?? Consts.IndependentCandidateColor;
                electionMapWinner.Winner.Party = winner.Party;
            }
            else
            {
                if (string.Equals(ballot.Election.Subtitle, "Invalidat"))
                {
                    electionMapWinner.Winner.Name = "NU AU VOTAT";
                    electionMapWinner.Winner.ShortName = "NU AU VOTAT";
                    electionMapWinner.Winner.Votes = turnoutForCountry.EligibleVoters - turnoutForCountry.TotalVotes;
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
            }

            electionMapWinner.ValidVotes = turnoutForCountry.ValidVotes;
            return electionMapWinner;
        }

        public List<CandidateResult> RetrieveWinners(List<CandidateResult> results,
            BallotType ballotType)
        {
            foreach (var candidateResult in results)
            {
                if (candidateResult.Party == null && candidateResult.PartyName.IsNotEmpty())
                    candidateResult.Party = new Party { Name = candidateResult.PartyName };
            }
            var groupedWinners = results
                .GroupBy(w => w.Party?.Name)
                .OrderByDescending(w => w.Count())
                .ToList();
            var candidateResults = new List<CandidateResult>();
            foreach (var candidate in groupedWinners)
            {
                var electionMapWinner = candidate.FirstOrDefault();
                var item = new CandidateResult
                {
                    Name = candidate.Key == null ? "INDEPENDENT" : electionMapWinner?.Party.Name,
                    Party = electionMapWinner?.Party
                };
                if (ballotType == BallotType.Mayor || ballotType == BallotType.CountyCouncilPresident)
                    item.Votes = candidate.Count();
                else
                    item.Votes = candidate.Sum(c => c.Votes);
                item.TotalSeats = item.SeatsGained = candidate.Sum(c => c.Seats1 + c.Seats2);
                candidateResults.Add(item);
            }

            return candidateResults;
        }

        public async Task<Result<List<CandidateResult>>> GetAllLocalityWinners(int ballotId)
        {
            var dbWinners = await GetWinners(ballotId, null, ElectionDivision.Locality);
            if (dbWinners.Count > 0)
                return dbWinners.Select(w => w.Candidate).ToList();
            _appCache.Remove(MemoryCache.CreateWinnersKey(ballotId, null, ElectionDivision.Locality));
            var winners = new List<CandidateResult>();
            var allLocalities = await _dbContext.Localities.ToListAsync();
            var resultsForElection = await _dbContext.CandidateResults
                .Include(c => c.Party)
                .Where(c => c.BallotId == ballotId && c.Division == ElectionDivision.Locality)
                .ToListAsync();

            var localitiesForThisElection = allLocalities
                .Where(l => resultsForElection.Any(r => r.LocalityId == l.LocalityId)).ToList();
            var turnouts = await _dbContext.Turnouts
                .Where(c => c.BallotId == ballotId &&
                            c.Division == ElectionDivision.Locality)
                .ToListAsync();
            List<Winner> winningCandidates = new List<Winner>();
            foreach (var locality in localitiesForThisElection)
            {
                var localityWinner = resultsForElection
                    .Where(c => c.LocalityId == locality.LocalityId)
                    .OrderByDescending(c => c.Votes)
                    .FirstOrDefault();
                if (localityWinner == null)
                    continue;
                var turnoutForLocality = turnouts
                    .FirstOrDefault(c => c.LocalityId == locality.LocalityId);
                winners.Add(localityWinner);
                winningCandidates.Add(CreateWinner(ballotId, null, localityWinner, turnoutForLocality, ElectionDivision.Locality));
            }

            await SaveWinners(winningCandidates);

            return Result.Success(winners);
        }

    }
}