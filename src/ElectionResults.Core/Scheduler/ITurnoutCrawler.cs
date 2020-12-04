using System.Threading.Tasks;

namespace ElectionResults.Core.Scheduler
{
    public interface ITurnoutCrawler
    {
        Task Import();
    }
}