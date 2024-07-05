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
                MemoryCache.Counties.Key, async () => await _dbContext.Counties
                    .Where(c => c.Name != "MINORITĂȚI")
                    .OrderBy(c => c.Name)
                    .ToListAsync(),
                DateTimeOffset.Now.AddMinutes(MemoryCache.Counties.Minutes));
        }

        public async Task<Result<List<Locality>>> GetLocalities(int? countyId, int? ballotId)
        {
            IQueryable<Locality> dbSet = _dbContext.Localities;
            if (!countyId.HasValue)
            {
                var localitiesResult = await _appCache.GetOrAddAsync(
                    MemoryCache.Localities.Key, () => _dbContext.Localities.OrderBy(c => c.Name).ToListAsync(),
                    DateTimeOffset.Now.AddMinutes(MemoryCache.Localities.Minutes));

                return localitiesResult;
            }

            dbSet = dbSet.Where(l => l.CountyId == countyId.Value);
            var localities = await dbSet.OrderBy(l => l.Name).ToListAsync();
            if (ballotId.HasValue)
            {
                var ids = await _dbContext.Turnouts.Where(c => c.BallotId == ballotId.Value)
                    .Select(c => c.LocalityId).ToListAsync();
                return localities.Where(l => ids.Any(i => i == l.LocalityId)).ToList();
            }

            return localities;
        }

        public async Task<Result<List<Country>>> GetCountries(int? ballotId)
        {
            var countries = await _appCache.GetOrAddAsync(
                MemoryCache.Countries.Key, () => _dbContext.Countries.OrderBy(c => c.Name).ToListAsync(),
                DateTimeOffset.Now.AddMinutes(MemoryCache.Countries.Minutes));
            if (ballotId.HasValue)
            {
                var ids = await _dbContext.Turnouts.Where(c => c.BallotId == ballotId.Value)
                    .Select(c => c.CountryId).Distinct().ToListAsync();
                return countries.Where(l => ids.Any(i => i == l.Id)).ToList();
            }

            return countries;
        }

        public async Task<Result<Locality>> GetLocalityById(int? localityId, bool includeCounty = false)
        {
            IQueryable<Locality> dbSet = _dbContext.Localities;
            if (includeCounty)
            {
                dbSet.Include(l => l.County);
            }

            dbSet = dbSet.Where(l => l.LocalityId == localityId);
            var localityKey = MemoryCache.Locality.Key + localityId + includeCounty;
            return await _appCache.GetOrAddAsync(
                localityKey, () => dbSet.Where(l => l.LocalityId == localityId)
                    .FirstOrDefaultAsync(),
                DateTimeOffset.Now.AddMinutes(MemoryCache.Locality.Minutes));
        }

        public async Task<Result<County>> GetCountyById(int? countyId)
        {
            IQueryable<County> dbSet = _dbContext.Counties;

            dbSet = dbSet.Where(l => l.CountyId == countyId);
            var countyKey = MemoryCache.Locality.Key + countyId;
            return await _appCache.GetOrAddAsync(
                countyKey, () => dbSet.Where(l => l.CountyId == countyId).FirstOrDefaultAsync(),
                DateTimeOffset.Now.AddMinutes(MemoryCache.County.Minutes));
        }
    }
}