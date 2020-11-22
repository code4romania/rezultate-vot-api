using System;
using System.Threading.Tasks;
using ElectionResults.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionResults.Core.Scheduler
{
    public class ScheduleTask : ScheduledProcessor
    {
        private readonly IParliamentCrawler _parliamentCrawler;

        public ScheduleTask(IServiceScopeFactory serviceScopeFactory,
            IParliamentCrawler parliamentCrawler)
            : base(serviceScopeFactory)
        {
            _parliamentCrawler = parliamentCrawler;
        }

        public override async Task ProcessInScope(IServiceProvider serviceProvider)
        {
            Log.LogInformation($"Importing candidates at {DateTime.UtcNow:F}");
            await _parliamentCrawler.Import();
        }
    }
}