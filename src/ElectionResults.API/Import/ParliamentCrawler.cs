using System;
using System.Threading.Tasks;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using ElectionResults.Core.Scheduler;
using ElectionResults.Importer;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionResults.API.Import
{
    public class ParliamentCrawler : IParliamentCrawler
    {
        private readonly IServiceProvider _serviceProvider;

        public ParliamentCrawler(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task Import()
        {
            try
            {
                using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
                {
                    await ParliamentImporter.Import(dbContext);
                }
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        }
    }
}
