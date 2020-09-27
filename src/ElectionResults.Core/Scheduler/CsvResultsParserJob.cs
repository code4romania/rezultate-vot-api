using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

namespace ElectionResults.Core.Scheduler
{
    public class CsvResultsParserJob : CsvGenericDownloadJob
    {
        public CsvResultsParserJob(IServiceProvider serviceProvider) : base(serviceProvider)
        {
            _csvUrl = "https://prezenta.roaep.ro/locale27092020/data/csv/simpv/presence_now.csv";
        }

        protected override Task ProcessStream(Stream stream)
        {
            throw new NotImplementedException();
        }
    }
}
