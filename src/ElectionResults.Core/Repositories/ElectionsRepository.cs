using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class ElectionsRepository : IElectionsRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAppCache _appCache;
        private readonly CacheSettings _cacheSettings;

        public ElectionsRepository(ApplicationDbContext dbContext, IAppCache appCache)
        {
            _dbContext = dbContext;
            _appCache = appCache;
            _cacheSettings = MemoryCache.Elections;
        }

        public async Task<Result<List<Election>>> GetAllElections(bool includeBallots = false)
        {
            var elections = await _appCache.GetOrAddAsync(
                _cacheSettings.Key, () => CreateQueryable(includeBallots).ToListAsync(),
                DateTimeOffset.Now.AddMinutes(_cacheSettings.Minutes));
            return Result.Success(elections);
        }

        private IQueryable<Election> CreateQueryable(bool includeBallots)
        {
            var query = _dbContext.Elections.AsNoTracking();
            if (includeBallots)
                query = query.Include(b => b.Ballots);
            return query;
        }

        public async Task<Result<List<ElectionBallot>>> GetElectionsForNewsFeed()
        {
            var ballots = await _dbContext.Ballots.AsNoTracking().Include(b => b.Election).ToListAsync();
            var ballotGroups = ballots.GroupBy(b => b.ElectionId).ToList();
            var electionBallots = new List<ElectionBallot>();

            foreach (var ballot in ballotGroups)
            {
                var electionBallot = new ElectionBallot();
                electionBallot.ElectionName =
                    $"{ballot.FirstOrDefault().Election.Name} - {ballot.FirstOrDefault().Date.Year}";
                electionBallot.ElectionId = ballot.Key;
                electionBallot.Ballots = new List<Ballot>();

                Ballot singleBallot;
                switch (ballot.FirstOrDefault().BallotType)
                {
                    case BallotType.Referendum:
                        singleBallot = ballot.FirstOrDefault();
                        singleBallot.Name = singleBallot.Election.Subtitle;
                        if (singleBallot.Name.Length > 100)
                            singleBallot.Name = singleBallot.Name.Substring(0, 100);
                        electionBallot.Ballots.Add(singleBallot);
                        electionBallots.Add(electionBallot);
                        continue;
                    case BallotType.President:
                        for (int i = 0; i < ballot.Count(); i++)
                        {
                            var round = ballot.ElementAt(i);
                            if (round.Round > 0)
                                round.Name = $"{round.Name}";
                            else
                                round.Name = round.Name;
                            electionBallot.Ballots.Add(round);
                        }
                        electionBallots.Add(electionBallot);
                        break;
                    case BallotType.Mayor:
                        for (int i = 0; i < ballot.Count(); i++)
                        {
                            var round = ballot.ElementAt(i);
                            if (round.Round > 0 && round.BallotType == BallotType.Mayor)
                                round.Name = $"Primar - {round.Name}";
                            else
                                round.Name = round.Name;
                            electionBallot.Ballots.Add(round);
                        }
                        electionBallots.Add(electionBallot);
                        break;
                    case BallotType.EuropeanParliament:
                        singleBallot = ballot.FirstOrDefault();
                        singleBallot.Name = $"{singleBallot.Election.Name} - {singleBallot.Date.Year}";
                        electionBallot.Ballots = new List<Ballot> { singleBallot };
                        electionBallots.Add(electionBallot);
                        break;
                    default:
                        electionBallot.Ballots = ballot.ToList();
                        electionBallots.Add(electionBallot);
                        continue;
                }
            }

            return electionBallots;
        }

    }
}
