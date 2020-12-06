using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using CsvHelper;
using ElectionResults.Core.Configuration;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using LazyCache;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;

namespace ElectionResults.Core.Scheduler
{
    public class ResultsCrawler : IResultsCrawler
    {
        private readonly IFileDownloader _fileDownloader;
        private readonly LiveElectionSettings _liveElectionSettings;
        private readonly IAppCache _appCache;
        private readonly IServiceProvider _serviceProvider;
        private readonly ILiveElectionUrlBuilder _liveElectionUrlBuilder;
        private int _eligibleVotersIndex;
        private int _totalVotesIndex;
        private int _nullVotesIndex;
        private int _validVotesIndex;
        private int _sirutaIndex;
        private List<Country> _dbCountries;
        private int _countryNameIndex;

        public ResultsCrawler(IFileDownloader fileDownloader,
            IOptions<LiveElectionSettings> options,
            IAppCache appCache,
            IServiceProvider serviceProvider,
            ILiveElectionUrlBuilder liveElectionUrlBuilder)
        {
            _fileDownloader = fileDownloader;
            _liveElectionSettings = options.Value;
            _appCache = appCache;
            _serviceProvider = serviceProvider;
            _liveElectionUrlBuilder = liveElectionUrlBuilder;
            _eligibleVotersIndex = 12;
            _totalVotesIndex = 16;
            _nullVotesIndex = 23;
            _validVotesIndex = 22;
            _sirutaIndex = 5;
            _countryNameIndex = 4;
        }

        public async Task<Result<LiveElectionInfo>> Import(string url)
        {
            var electionInfo = await GetCandidatesFromUrl(url);
            if (electionInfo.Candidates == null)
                return Result.Failure<LiveElectionInfo>("File doesn't exist");
            return electionInfo;
        }

        public async Task ImportAllResults()
        {
          
        }

        public async Task<LiveElectionInfo> AggregateNationalResults(ElectionResultsQuery query, Ballot ballot)
        {
            var electionInfo = new LiveElectionInfo();
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var dbCounties = await dbContext.Counties.ToListAsync();
                var turnouts = await dbContext.Turnouts.Where(t => t.BallotId == ballot.BallotId).ToListAsync();
                var results = new List<CandidateResult>();
                foreach (var countyTurnout in turnouts.Where(t => t.Division == ElectionDivision.County))
                {
                    var county = dbCounties.First(c => c.CountyId == countyTurnout.CountyId);
                    if (county == null)
                    {
                        continue;
                    }
                    var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.County, county.ShortName, null);

                    var countyResults = await Import(url.Value);
                    if (countyResults.IsSuccess)
                    {
                        Console.WriteLine($"Added {county.Name}");
                        results.AddRange(countyResults.Value.Candidates);
                        electionInfo.ValidVotes += countyResults.Value.ValidVotes;
                        electionInfo.NullVotes += countyResults.Value.NullVotes;
                    }
                }

                var grouped = results.GroupBy(c => c.Name).OrderByDescending(p => p.Sum(p => p.Votes)).ToList();
                var candidateResults = grouped.Select(g =>
                {
                    var candidate = g.FirstOrDefault();
                    candidate.Votes = g.Sum(c => c.Votes);
                    return candidate;
                }).ToList();
                electionInfo.Candidates = candidateResults;
                electionInfo.TotalVotes = candidateResults.Sum(c => c.Votes);
            }

            return electionInfo;
        }

        public async Task<LiveElectionInfo> AggregateDiasporaResults(ElectionResultsQuery query, Ballot ballot)
        {
            var electionInfo = new LiveElectionInfo();
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var results = new List<CandidateResult>();

                var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.Diaspora, null, null);

                var diasporaResults = await _appCache.GetOrAddAsync(
                    $"{url}", () => Import(url.Value),
                    DateTimeOffset.Now.AddMinutes(_liveElectionSettings.CsvCacheInMinutes));
                if (diasporaResults.IsSuccess)
                {
                    results.AddRange(diasporaResults.Value.Candidates);
                    electionInfo.ValidVotes += diasporaResults.Value.ValidVotes;
                    electionInfo.NullVotes += diasporaResults.Value.NullVotes;
                }

                var grouped = results.GroupBy(c => c.Name).OrderByDescending(p => p.Sum(p => p.Votes)).ToList();
                var candidateResults = grouped.Select(g =>
                {
                    var candidate = g.FirstOrDefault();
                    candidate.Votes = g.Sum(c => c.Votes);
                    return candidate;
                }).ToList();
                electionInfo.Candidates = candidateResults;
                electionInfo.TotalVotes = candidateResults.Sum(c => c.Votes);
            }

            return electionInfo;
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
            var siruta = 0;

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
                        ValidVotes = valid,
                        Siruta = siruta
                    };
                var index = 0;
                total += int.Parse(csvParser.GetField(_eligibleVotersIndex));
                voted += int.Parse(csvParser.GetField(_totalVotesIndex));
                nullVotes += int.Parse(csvParser.GetField(_nullVotesIndex));
                valid += int.Parse(csvParser.GetField(_validVotesIndex));
                siruta = int.Parse(csvParser.GetField(_sirutaIndex));
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
            var index = 25;
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

        protected async Task<string> ReadCsvContent(Stream csvStream)
        {
            var buffer = new byte[csvStream.Length];
            await csvStream.ReadAsync(buffer, 0, (int)csvStream.Length);
            var csvContent = Encoding.UTF8.GetString(buffer);
            return csvContent;
        }
    }

}