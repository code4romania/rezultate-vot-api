using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.RoAep;
using ElectionResults.Hangfire.Apis.RoAep.Models;
using ElectionResults.Hangfire.Extensions;
using Z.EntityFramework.Plus;

namespace ElectionResults.Hangfire.Jobs;
public class CheckStaticDataJob(IRoAepApi api, ApplicationDbContext context, ILogger<CheckStaticDataJob> logger)
{
    public async Task Run(string electionRoundId, bool hasDiaspora, CancellationToken ct = default)
    {
        var counties = await api.ListCounties(electionRoundId);
        var existingCounties = (await context.Counties.FromCacheAsync(ct, CacheKeys.RoCounties)).ToList();
        var existingLocalities = (await context.Localities.FromCacheAsync(ct, CacheKeys.RoLocalities)).ToList();

        foreach (var county in counties)
        {
            if (county.Code.InvariantEquals("B"))
            {
                var localities = await api.ListLocalities(electionRoundId, county.Code);

                var countyId = existingCounties.First(dbCounty => CountiesAreEqual(dbCounty, county)).CountyId;

                foreach (var locality in localities)
                {
                    CheckLocality(county, existingLocalities.Where(l => l.CountyId == countyId).ToList(), locality);
                }
            }
            else
            {
                var uats = await api.ListUats(electionRoundId, county.Code);

                var countyId = existingCounties.First(dbCounty => CountiesAreEqual(dbCounty, county)).CountyId;

                foreach (var uat in uats)
                {
                    CheckUat(county, existingLocalities.Where(l => l.CountyId == countyId).ToList(), uat);
                }
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

    private void CheckUat(CountyModel county, List<Locality> existingUats, UatModel uat)
    {
        var uatExists = existingUats.Any(bdUat => bdUat.Siruta == uat.Siruta);

        if (!uatExists)
        {
            logger.LogWarning("{countyCode} - UAT not exists {@uat}", county.Code, uat);
        }
    }
    private void CheckLocality(CountyModel county, List<Locality> existingLocalities, LocalityModel locality)
    {
        var localityExits = existingLocalities.Any(dbLocality => dbLocality.Siruta.ToString() == locality.Code);

        if (!localityExits)
        {
            logger.LogWarning("{countyCode} - UAT not exists {@uat}", county.Code, locality);
        }
    }
}