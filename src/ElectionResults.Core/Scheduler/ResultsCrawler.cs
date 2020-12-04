using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using CsvHelper;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Infrastructure;
using Microsoft.Extensions.Options;

namespace ElectionResults.Core.Scheduler
{
    public class ResultsCrawler : IResultsCrawler
    {
        private readonly IFileDownloader _fileDownloader;
        private HttpClient _httpClient;

        public ResultsCrawler(IFileDownloader fileDownloader)
        {
            _fileDownloader = fileDownloader;
        }

        public async Task<Result<LiveElectionInfo>> Import(string url)
        {
            return await GetCandidatesFromUrl(url);
        }

        public async Task<LiveElectionInfo> GetCandidatesFromUrl(string url)
        {
            try
            {
                var stream = await _fileDownloader.Download(url);
                var liveElectionInfo = await ExtractCandidatesFromCsv(stream);
                return liveElectionInfo;
            }
            catch
            {
                return LiveElectionInfo.Default;
            }
        }

        protected async Task<string> ReadCsvContent(Stream csvStream)
        {
            var buffer = new byte[csvStream.Length];
            await csvStream.ReadAsync(buffer, 0, (int)csvStream.Length);
            var csvContent = Encoding.UTF8.GetString(buffer);
            return csvContent;
        }

        private async Task<LiveElectionInfo> ExtractCandidatesFromCsv(Stream csvStream)
        {
            List<CandidateResult> candidates;
            var csvContent = await ReadCsvContent(csvStream);
            TextReader sr = new StringReader(csvContent);
            var csvParser = new CsvReader(sr, CultureInfo.CurrentCulture);
            csvParser.Configuration.HeaderValidated = null;
            csvParser.Configuration.MissingFieldFound = null;
            candidates = await GetCandidates(csvParser);
            var nullVotes = 0;
            var total = 0;
            var voted = 0;
            var valid = 0;
            
            while (true)
            {
                var result = await csvParser.ReadAsync();
                if (!result)
                    return new LiveElectionInfo
                    {
                        Candidates = candidates,
                        EligibleVoters = total,
                        TotalVotes = voted,
                        NullVotes = nullVotes,
                        ValidVotes = valid
                    };
                var index = 0;
                total += int.Parse((csvParser.GetField(12)));
                voted += int.Parse((csvParser.GetField(17)));
                nullVotes += int.Parse((csvParser.GetField(23)));
                valid += int.Parse((csvParser.GetField(22)));
                for (int i = 25; i < 25 + candidates.Count; i++)
                {
                    try
                    {
                        var votes = csvParser.GetField(i);
                        candidates[index].Votes += int.Parse(votes);
                        index++;
                    }
                    catch (Exception)
                    {
                        break;
                    }
                }
            }
        }

        private async Task<List<CandidateResult>> GetCandidates(CsvReader csvParser)
        {
            var readAsync = await csvParser.ReadAsync();
            var candidates = new List<CandidateResult>();
            var index = 26;
            while (true)
            {
                try
                {
                    var field = csvParser.GetField(index++);
                    field = field.Replace("-voturi", "");
                    candidates.Add(new CandidateResult
                    {
                        Name = field
                    });
                }
                catch (Exception)
                {
                    return candidates;
                }
            }
        }

    }
}