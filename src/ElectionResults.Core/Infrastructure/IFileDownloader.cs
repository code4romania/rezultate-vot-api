using System.IO;
using System.Threading.Tasks;

namespace ElectionResults.Core.Infrastructure
{
    public interface IFileDownloader
    {
        Task<Stream> Download(string url);
    }
}