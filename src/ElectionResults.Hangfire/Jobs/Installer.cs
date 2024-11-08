using ElectionResults.Hangfire.Apis.RoAep.SicpvModels;
using ElectionResults.Hangfire.Options;
using Hangfire;
using Microsoft.Extensions.Options;

namespace ElectionResults.Hangfire.Jobs;

public static class Installer
{
    public static IServiceCollection RegisterJobs(this IServiceCollection services)
    {
        services.AddScoped<SeedData>();
        //services.AddScoped<SyncEuroTurnoutsJob>();
        services.AddScoped<DownloadAndProcessTurnoutResultsJob>();
        services.AddScoped<DownloadVoteMonitorStatisticsJob>();

        return services;
    }


    public static WebApplication WithJobs(this WebApplication app)
    {
        // Create a new scope to retrieve scoped services
        using var scope = app.Services.CreateScope();

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var crawlerOptions = app.Services.GetRequiredService<IOptions<CrawlerOptions>>()!;

        backgroundJobClient.Enqueue<SeedData>(x => x.Run(CancellationToken.None));

        recurringJobManager.AddOrUpdate<DownloadAndProcessTurnoutResultsJob>($"locale09062024-data-processor", x => x.Run("locale09062024", 50, false, StageCode.FINAL), "* */12 * * *");

        // recurringJobManager.AddOrUpdate<DownloadAndProcessTurnoutResultsJob>($"europarlamentare09062024-data-processor", x => x.Run("europarlamentare09062024", 51, true, StageCode.PROV), "*/5 * * * *");

        // var electionRoundIds = crawlerOptions.Value.ElectionRounds.Select(x => x.ElectionRoundId).ToList();
        // var voteMonitorElectionRoundId = crawlerOptions.Value.ElectionRoundId;
        //
        // recurringJobManager
        //     .AddOrUpdate<DownloadVoteMonitorStatisticsJob>("vote-monitor-statistics", x => x.Run(electionRoundIds, voteMonitorElectionRoundId, CancellationToken.None), "*/15 * * * *");

        return app;
    }
}