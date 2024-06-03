using System.Text.Json;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.RoAep;
using ElectionResults.Hangfire.Apis.RoAep.Models;
using ElectionResults.Hangfire.Extensions;
using Z.EntityFramework.Plus;
using Diacritics;

namespace ElectionResults.Hangfire.Jobs;
public class CheckStaticDataJob(IRoAepApi api, ApplicationDbContext context, ILogger<CheckStaticDataJob> logger) : ICheckStaticDataJob
{
    public async Task Run(string electionRoundId, CancellationToken ct = default)
    {
        var counties = await api.ListCounties(electionRoundId);
        var existingCounties = (await context.Counties.FromCacheAsync(ct, CacheKeys.Counties)).ToList();
        var existingLocalities = (await context.Localities.FromCacheAsync(ct, CacheKeys.Localities)).ToList();

        foreach (var county in counties)
        {
            var localities = await api.ListLocalities(electionRoundId, county.Code);

            var countyId = existingCounties.First(dbCounty => CountiesAreEqual(dbCounty, county)).CountyId;

            foreach (var locality in localities)
            {
                CheckLocality(county, existingLocalities.Where(l => l.CountyId == countyId).ToList(), locality);
            }
        }
    }

    private static bool CountiesAreEqual(County dbCounty, CountyModel county)
    {
        if (county.Code.InvariantEquals("B"))
        {
            return dbCounty.ShortName.InvariantEquals(county.Code);
        }

        return dbCounty.Name.InvariantEquals(county.Name) && dbCounty.ShortName.InvariantEquals(county.Code);
    }

    private static bool LocalitiesAreEqual(Locality dbLocality, LocalityModel locality)
    {
        if (locality.Name.ToUpper().StartsWith("BUCUREŞTI"))
        {
            return dbLocality.Name.InvariantEquals(locality.Name.Replace("BUCUREŞTI", "").Trim());
        }
        return dbLocality.Name.InvariantEquals(locality.Name);
    }

    private void CheckLocality(CountyModel county, List<Locality> existingLocalities, LocalityModel locality)
    {
        var localityExists = existingLocalities.Any(x => LocalitiesAreEqual(x, locality));

        if (!localityExists)
        {
            logger.LogWarning("{countyCode} - Locality not exists {@locality}", county.Code, locality);
        }
    }
}