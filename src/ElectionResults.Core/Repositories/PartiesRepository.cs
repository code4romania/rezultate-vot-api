using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class PartiesRepository : IPartiesRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAppCache _appCache;
        private readonly CacheSettings _cacheSettings;

        public PartiesRepository(ApplicationDbContext dbContext, IAppCache appCache)
        {
            _dbContext = dbContext;
            _appCache = appCache;
            _cacheSettings = MemoryCache.Parties;
        }

        public async Task<IEnumerable<Party>> GetAllParties()
        {
            return await _appCache.GetOrAddAsync(
                _cacheSettings.Key, async () =>await _dbContext.Parties.ToListAsync(),
                DateTimeOffset.Now.AddMinutes(_cacheSettings.Minutes));
        }
    }
}
