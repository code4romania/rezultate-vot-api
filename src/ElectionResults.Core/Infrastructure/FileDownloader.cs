using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using ElectionResults.Core.Configuration;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Infrastructure;
using Microsoft.Extensions.Options;

namespace ElectionResults.API.Import
{
    public class FileDownloader : IFileDownloader
    {
        private LiveElectionSettings _settings;

        public FileDownloader(IOptions<LiveElectionSettings> options)
        {
            _settings = options.Value;
        }

        public async Task<Stream> Download(string url)
        {
            try
            {
                var httpClientHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                if (_settings.FtpUser.IsNotEmpty() && _settings.FtpPassword.IsNotEmpty())
                {
                    httpClientHandler.Credentials = new NetworkCredential(_settings.FtpUser, _settings.FtpPassword);
                }
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
                var httpClient = new HttpClient(httpClientHandler);
                var response = await httpClient.GetStringAsync(url);
                return new MemoryStream(Encoding.UTF8.GetBytes(response));
            }
            catch (Exception e)
            {
                Log.LogError(e, $"Failed to download file: {url}");
                throw;
            }
        }
    }
}
