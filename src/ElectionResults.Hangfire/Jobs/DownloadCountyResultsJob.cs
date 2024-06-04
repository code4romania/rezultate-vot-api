using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.RoAep;
using ElectionResults.Hangfire.Apis.RoAep.Models;

namespace ElectionResults.Hangfire.Jobs;

public class DownloadCountyResultsJob(IRoAepApi roAepApi, ApplicationDbContext context, ILogger<DownloadCountyResultsJob> logger)
{
    public async Task Run(string electionRoundKey, int electionRoundId, string countyCode, bool isDiaspora, CancellationToken ct)
    {
        logger.LogInformation("Downloading data for {countyCode}", countyCode);
        var result = await roAepApi.GetPVForCounty(electionRoundKey, countyCode, StageCode.FINAL);

        logger.LogInformation("Finished processing data data for {countyCode}", countyCode);
    }
}