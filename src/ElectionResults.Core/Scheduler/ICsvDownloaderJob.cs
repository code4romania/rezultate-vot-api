using System.Threading.Tasks;

namespace ElectionResults.Core.Scheduler
{
    public interface ICsvDownloaderJob
    {
        Task DownloadFiles();

        Task<LiveElectionInfo> GetCandidatesFromUrl(string url);
    }
}