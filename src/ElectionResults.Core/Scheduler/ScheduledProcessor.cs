using System;
using System.Threading;
using System.Threading.Tasks;
using ElectionResults.Core.BackgroundService;
using ElectionResults.Core.Infrastructure;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionResults.Core.Scheduler
{
    public abstract class ScheduledProcessor : ScopedProcessor
    {
        private DateTime _nextRun;
        private int _intervalInSeconds = 3600;

        public ScheduledProcessor(IServiceScopeFactory serviceScopeFactory) : base(serviceScopeFactory)
        {
            _nextRun = DateTime.Now.AddHours(1);
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
                        _nextRun = DateTime.Now.AddSeconds(_intervalInSeconds);
                        Log.LogInformation($"Next run will be at {_nextRun:F}");
                    }
                } while (!stoppingToken.IsCancellationRequested);
            });
        }
    }
}
