using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;
using Z.EntityFramework.Plus;

namespace ElectionResults.Core.Repositories
{
    public class PartiesRepository : IPartiesRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly CacheSettings _cacheSettings;

        public PartiesRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
            _cacheSettings = MemoryCache.Parties;
        }

        public async Task<List<Party>> GetAllParties()
        {
            return (await _dbContext.Parties.FromCacheAsync()).ToList();
        }
    }
}