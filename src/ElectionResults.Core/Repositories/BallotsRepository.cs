using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class BallotsRepository : IBallotsRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAppCache _appCache;
        private readonly CacheSettings _cacheSettings;

        public BallotsRepository(ApplicationDbContext dbContext, IAppCache appCache)
        {
            _dbContext = dbContext;
            _appCache = appCache;
            _cacheSettings = MemoryCache.Ballots;
        }

        public async Task<IEnumerable<Ballot>> GetAllBallots(bool includeElection = false)
        {
            return await _appCache.GetOrAddAsync(
                _cacheSettings.Key, async () =>
                {
                    var query = CreateQueryable(includeElection);
                    return  await query.ToListAsync();
                },
                DateTimeOffset.Now.AddMinutes(_cacheSettings.Minutes));
        }

        public async Task<Ballot> GetBallotById(int ballotId, bool includeElection = false)
        {
            var ballot = await _appCache.GetOrAddAsync(
                _cacheSettings.Key, async () =>
                {
                    var query = CreateQueryable(includeElection);
                    return await query.Where(b => b.BallotId == ballotId).FirstOrDefaultAsync();
                },
                DateTimeOffset.Now.AddMinutes(_cacheSettings.Minutes));
            return ballot;
        }

        private IQueryable<Ballot> CreateQueryable(bool includeElection)
        {
            var query = _dbContext.Ballots.AsNoTracking();
            if (includeElection)
                query = query.Include(b => b.Election);
            return query;
        }
    }
}