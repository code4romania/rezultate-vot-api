using ElectionResults.Hangfire.Options;
using Hangfire;
using HangfireBasicAuthenticationFilter;
using Microsoft.Extensions.Options;

namespace ElectionResults.Hangfire;

public static class HangfireExtensions
{
    public static WebApplication WithHangfireDashboard(this WebApplication app)
    {
        var hangfireOptions = app.Services.GetRequiredService<IOptions<HangfireDashboardOptions>>()!;

        if (hangfireOptions.Value.IsSecuredDashboard)
        {
            app.UseHangfireDashboard("/hangfire", new DashboardOptions
            {
                DashboardTitle = "ElectionResults.Hangfire",
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
}