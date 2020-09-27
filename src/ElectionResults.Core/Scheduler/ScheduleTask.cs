using System;
using System.Threading.Tasks;
using ElectionResults.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionResults.Core.Scheduler
{
    public class ScheduleTask : ScheduledProcessor
    {
        private readonly ICsvDownloaderJob _csvDownloaderJob;

        public ScheduleTask(IServiceScopeFactory serviceScopeFactory,
            ICsvDownloaderJob csvDownloaderJob)
            : base(serviceScopeFactory)
        {
            _csvDownloaderJob = csvDownloaderJob;
        }

        public override async Task ProcessInScope(IServiceProvider serviceProvider)
        {
            Log.LogInformation($"Processing starts here at {DateTime.UtcNow:F}");
            await _csvDownloaderJob.DownloadFiles();
        }
    }
}