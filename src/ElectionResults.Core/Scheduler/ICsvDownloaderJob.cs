using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Scheduler
{
    public interface ICsvDownloaderJob
    {
        Task DownloadFiles();

        Task<LiveElectionInfo> GetCandidatesFromUrl(string url);
    }
}