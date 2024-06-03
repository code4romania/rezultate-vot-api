using ElectionResults.Hangfire.Jobs;
using ElectionResults.Hangfire.Options;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.Extensions.Options;

namespace ElectionResults.Hangfire;

public static class WebApplicationExtensions
{
    public static WebApplication WithHangfireDashboard(this WebApplication app)
    {
        var hangfireOptions = app.Services.GetRequiredService<IOptions<HangfireDashboardOptions>>()!;

        if (hangfireOptions.Value.IsSecuredDashboard)
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = "Vote.Monitor.Hangfire",
                Authorization = new[]
                {
                    new HangfireCustomBasicAuthenticationFilter{
                        User = hangfireOptions.Value.Username,
                        Pass = hangfireOptions.Value.Password
                    }
                }
            });
        }
        else
        {
            app.UseHangfireDashboard();
        }

        return app;
    }

    public static WebApplication WithJobs(this WebApplication app)
    {
        // Create a new scope to retrieve scoped services
        using var scope = app.Services.CreateScope();
        var recurringJobManager = scope.ServiceProvider.GetRequiredService<IRecurringJobManager>();

        var backgroundJobClient = scope.ServiceProvider.GetRequiredService<IBackgroundJobClient>();

        var roAepOptions = app.Services.GetRequiredService<IOptions<RoAepOptions>>()!;
        foreach (var electionRoundKey in roAepOptions.Value.ElectionRoundKeys)
        {
            backgroundJobClient.Enqueue<CheckStaticDataJob>(x => x.Run(electionRoundKey, CancellationToken.None));

            recurringJobManager.AddOrUpdate<IDownloadStuffJob>($"{electionRoundKey}-", x => x.Run(electionRoundKey, CancellationToken.None), "*/5 * * * *");
        }



        return app;
    }
}