using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis;

namespace ElectionResults.Hangfire.Jobs;

public class SyncEuroTurnoutsJob
{
    private readonly ITurnoutCrawler _turnoutCrawler;

    public SyncEuroTurnoutsJob(ITurnoutCrawler turnoutCrawler, ApplicationDbContext context)
    {
        _turnoutCrawler = turnoutCrawler;
    }

    public async Task Run(CancellationToken ct = default)
    {
        await _turnoutCrawler.InsertEuroTurnouts();
    }
}