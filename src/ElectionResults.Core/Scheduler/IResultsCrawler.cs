using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace ElectionResults.Core.Scheduler
{
    public interface IResultsCrawler
    {
        Task<Result<LiveElectionInfo>> Import(string url);
    }
}