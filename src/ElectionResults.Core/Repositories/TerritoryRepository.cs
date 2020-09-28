using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;
using LazyCache;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class TerritoryRepository : ITerritoryRepository
    {
        private readonly ApplicationDbContext _dbContext;
        private readonly IAppCache _appCache;

        public TerritoryRepository(ApplicationDbContext dbContext, IAppCache appCache)
        {
            _dbContext = dbContext;
            _appCache = appCache;
        }

        public async Task<Result<List<County>>> GetCounties()
        {
            return await _appCache.GetOrAddAsync(
                MemoryCache.Counties.Key, () => _dbContext.Counties.OrderBy(c => c.Name).ToListAsync(),
                DateTimeOffset.Now.AddMinutes(MemoryCache.Counties.Minutes));
        }

        public async Task<Result<List<Locality>>> GetLocalities(int? countyId)
        {
            IQueryable<Locality> dbSet = _dbContext.Localities;
            if (!countyId.HasValue)
            {
                return await _appCache.GetOrAddAsync(
                    MemoryCache.Localities.Key, () => _dbContext.Localities.OrderBy(c => c.Name).ToListAsync(),
                    DateTimeOffset.Now.AddMinutes(MemoryCache.Localities.Minutes));
            }

            dbSet = dbSet.Where(l => l.CountyId == countyId.Value);
            var localities = await dbSet.OrderBy(l => l.Name).ToListAsync();
            return localities;
        }

        public async Task<Result<List<Country>>> GetCountries()
        {
            return await _appCache.GetOrAddAsync(
                MemoryCache.Countries.Key, () => _dbContext.Countries.OrderBy(c => c.Name).ToListAsync(),
                DateTimeOffset.Now.AddMinutes(MemoryCache.Counties.Minutes));
        }
    }
}
