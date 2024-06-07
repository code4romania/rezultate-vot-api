
namespace ElectionResults.Hangfire.Options;

public static class Installer
{
    public static WebApplicationBuilder ConfigureAppOptions(this WebApplicationBuilder builder)
    {
        builder.Services.AddOptions();

        builder.Services.Configure<HangfireDashboardOptions>(builder.Configuration.GetSection(HangfireDashboardOptions.SectionKey));
        builder.Services.Configure<CrawlerOptions>(builder.Configuration.GetSection(CrawlerOptions.SectionKey));
        builder.Services.Configure<VoteMonitorOptions>(builder.Configuration.GetSection(VoteMonitorOptions.SectionKey));

        return builder;
    }
}