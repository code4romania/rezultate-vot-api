namespace ElectionResults.Hangfire.Jobs;

public static class Installer
{
    public static IServiceCollection RegisterJobs(this IServiceCollection services)
    {
        services.AddScoped<ICheckStaticDataJob, CheckStaticDataJob>();
        services.AddScoped<IDownloadStuffJob, DownloadStuffJob>();

        return services;
    }
}