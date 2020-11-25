using System.Threading.Tasks;

namespace ElectionResults.Core.Scheduler
{
    public interface IParliamentCrawler
    {
        Task Import();
    }
}