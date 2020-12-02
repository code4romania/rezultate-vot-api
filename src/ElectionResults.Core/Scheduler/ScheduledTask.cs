using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionResults.Core.Scheduler
{
    public class ScheduledTask : ScheduledProcessor
    {
        private readonly ITurnoutCrawler _turnoutCrawler;

        public ScheduledTask(IServiceScopeFactory serviceScopeFactory,
            ITurnoutCrawler turnoutCrawler)
            : base(serviceScopeFactory)
        {
            _turnoutCrawler = turnoutCrawler;
        }

        public override async Task ProcessInScope(IServiceProvider serviceProvider)
        {
            await _turnoutCrawler.Import();
        }
    }
}