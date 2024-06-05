using ElectionResults.Hangfire.Options;
using Hangfire;
using Microsoft.Extensions.Options;

namespace ElectionResults.Hangfire.Jobs;

public static class Installer
{
    public static IServiceCollection RegisterJobs(this IServiceCollection services)
    {
        services.AddScoped<CheckStaticDataJob>();
        services.AddScoped<DownloadAndProcessDataJob>();

        return services;
    }


    public static WebApplication WithJobs(this WebApplication app)
    {
        // Create a new scope to retrieve scoped services
        using var scope = app.Services.CreateScope();

        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();
        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var roAepOptions = app.Services.GetRequiredService<IOptions<CrawlerOptions>>()!;
        foreach (var electionRoundConfig in roAepOptions.Value.ElectionRounds)
        {
            backgroundJobClient.Enqueue<CheckStaticDataJob>(x => x.Run(electionRoundConfig.Key, electionRoundConfig.HasDiaspora, CancellationToken.None));

            recurringJobManager.AddOrUpdate<DownloadAndProcessDataJob>($"{electionRoundConfig.Key}-data-processor", x => x.Run(electionRoundConfig.Key, electionRoundConfig.ElectionRoundId, electionRoundConfig.HasDiaspora, CancellationToken.None), electionRoundConfig.CronExpression);
        }

        return app;
    }
}