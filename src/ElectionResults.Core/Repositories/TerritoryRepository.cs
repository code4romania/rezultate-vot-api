using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Z.EntityFramework.Plus;

namespace ElectionResults.Core.Repositories
{
    public class TerritoryRepository : ITerritoryRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public TerritoryRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<Result<List<County>>> GetCounties()
        {
            var counties = await _dbContext.Counties
                .Where(c => c.Name != "MINORITĂȚI")
                .OrderBy(c => c.Name)
                .FromCacheAsync();

            return counties.ToList();
        }

        public async Task<Result<List<Locality>>> GetLocalities(int? countyId, int? ballotId)
        {
            var localities = (await _dbContext.Localities.FromCacheAsync()).ToList();
            if (!countyId.HasValue)
            {
                var localitiesResult = localities.OrderBy(c => c.Name).ToList();

                return localitiesResult;
            }

            localities = localities.Where(l => l.CountyId == countyId.Value).OrderBy(l => l.Name).ToList();
            if (ballotId.HasValue)
            {
                var ids = await _dbContext
                    .Turnouts
                    .Where(c => c.BallotId == ballotId.Value)
                    .Select(c => c.LocalityId)
                    .ToListAsync();

                return localities.Where(l => ids.Any(i => i == l.LocalityId)).ToList();
            }

            return localities;
        }

        public async Task<Result<List<Country>>> GetCountries(int? ballotId)
        {
            var countries = (await _dbContext.Countries.OrderBy(c => c.Name).FromCacheAsync()).ToList();

            if (ballotId.HasValue)
            {
                var ids = await _dbContext.Turnouts
                    .Where(c => c.BallotId == ballotId.Value)
                    .Select(c => c.CountryId)
                    .Distinct()
                    .ToListAsync();

                return countries.Where(l => ids.Any(i => i == l.Id)).ToList();
            }

            return countries;
        }

        public async Task<Result<County>> GetCountyById(int? countyId)
        {
            var county = await _dbContext.Counties
                .DeferredFirstOrDefault(l => l.CountyId == countyId)
                .FromCacheAsync();

            return county;
        }
    }
}