using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.RoAep;
using Z.EntityFramework.Plus;

namespace ElectionResults.Hangfire.Jobs;

public class DownloadStuffJob(IRoAepApi roAepApi, ApplicationDbContext context, ILogger<DownloadStuffJob> logger) : IDownloadStuffJob
{
    public async Task Run(string electionRoundId, CancellationToken ct)
    {
        var counties = (await context.Counties.FromCacheAsync(ct, CacheKeys.Counties)).ToList();

        foreach (var county in counties)
        {
            logger.LogInformation("Downloading data for {countyCode}", county.ShortName);
            var result = await roAepApi.GetPVForCounty(electionRoundId, county.ShortName, Stage.FINAL);

            int a = 0;
        }

    }
}