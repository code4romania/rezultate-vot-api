using ElectionResults.Hangfire.Options;
using Hangfire;
using Microsoft.Extensions.Options;

namespace ElectionResults.Hangfire.Jobs;

public static class Installer
{
    public static IServiceCollection RegisterJobs(this IServiceCollection services)
    {
        services.AddScoped<CheckStaticDataJob>();
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


        foreach (var electionRoundConfig in crawlerOptions.Value.ElectionRounds)
        {
            backgroundJobClient.Enqueue<CheckStaticDataJob>(x => x.Run(electionRoundConfig.Key, electionRoundConfig.HasDiaspora, CancellationToken.None));

            recurringJobManager.AddOrUpdate<DownloadAndProcessTurnoutResultsJob>($"{electionRoundConfig.Key}-data-processor", x => x.Run(electionRoundConfig.Key, electionRoundConfig.ElectionRoundId, electionRoundConfig.HasDiaspora), electionRoundConfig.CronExpression);
        }
        var electionRoundIds = crawlerOptions.Value.ElectionRounds.Select(x => x.ElectionRoundId).ToList();
        var voteMonitorElectionRoundId = crawlerOptions.Value.ElectionRoundId;

        recurringJobManager
            .AddOrUpdate<DownloadVoteMonitorStatisticsJob>("vote-monitor-statistics", x => x.Run(electionRoundIds, voteMonitorElectionRoundId, CancellationToken.None), "*/15 * * * *");

        return app;
    }
}