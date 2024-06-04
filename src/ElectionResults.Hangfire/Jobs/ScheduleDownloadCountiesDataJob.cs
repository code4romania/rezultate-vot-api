using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using Hangfire;
using Z.EntityFramework.Plus;

namespace ElectionResults.Hangfire.Jobs;

public class ScheduleDownloadCountiesDataJob(ApplicationDbContext context, IBackgroundJobClient backgroundJobClient)
{
    public async Task Run(string electionRoundKey, int electionRoundId, ElectionCategory category, bool hasDiaspora, CancellationToken ct)
    {
        var electionRound = context.Elections.FirstOrDefault(x => x.ElectionId == electionRoundId);
        if (electionRound == null)
        {
            throw new ArgumentException($"Election round {electionRoundId} does not exist!");
        }

        var counties = (await context.Counties.FromCacheAsync(ct, CacheKeys.Counties)).ToList();

        foreach (var county in counties)
        {
            backgroundJobClient.Enqueue<DownloadCountyResultsJob>(x => x.Run(electionRoundKey, electionRoundId, county.ShortName, false, ct));
        }

        if (hasDiaspora)
        {
            backgroundJobClient.Enqueue<DownloadCountyResultsJob>(x => x.Run(electionRoundKey, electionRoundId, "SR", true, ct));
        }
    }
}