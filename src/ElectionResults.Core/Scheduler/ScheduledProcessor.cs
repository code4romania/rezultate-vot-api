using System;
using System.Threading;
using System.Threading.Tasks;
using ElectionResults.Core.BackgroundService;
using ElectionResults.Core.Configuration;
using ElectionResults.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ElectionResults.Core.Scheduler
{
    public abstract class ScheduledProcessor : ScopedProcessor
    {
        private DateTime _nextRun;
        private LiveElectionSettings _settings;

        public ScheduledProcessor(IServiceScopeFactory serviceScopeFactory, IOptions<LiveElectionSettings> options) : base(serviceScopeFactory)
        {
            _nextRun = DateTime.Now.AddSeconds(1);
            _settings = options.Value;
            Log.LogInformation($"Next run will be at {_nextRun:F}");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            return Task.Run(async () =>
            {
                do
                {
                    var now = DateTime.Now;
                    if (now > _nextRun)
                    {
                        await Process();
                        _nextRun = DateTime.Now.AddMinutes(_settings.TurnoutIntervalInMinutes);
                        Log.LogInformation($"Next run will be at {_nextRun:F}");
                    }
                } while (!stoppingToken.IsCancellationRequested);
            });
        }
    }
}
