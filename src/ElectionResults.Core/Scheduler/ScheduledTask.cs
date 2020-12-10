using System;
using System.Threading.Tasks;
using ElectionResults.Core.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ElectionResults.Core.Scheduler
{
    public class ScheduledTask : ScheduledProcessor
    {
        private readonly ITurnoutCrawler _turnoutCrawler;
        private readonly IResultsCrawler _resultsCrawler;

        public ScheduledTask(IServiceScopeFactory serviceScopeFactory,
            ITurnoutCrawler turnoutCrawler,
            IResultsCrawler resultsCrawler,
            IOptions<LiveElectionSettings> options)
            : base(serviceScopeFactory, options)
        {
            _turnoutCrawler = turnoutCrawler;
            _resultsCrawler = resultsCrawler;
        }

        public override async Task ProcessInScope(IServiceProvider serviceProvider)
        {
            await _resultsCrawler.ImportAll();
        }
    }
}