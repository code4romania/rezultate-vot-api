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
        private int _candidatesIndex;
        private int _nullVotesIndex2;

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
            SetIndexesForNationalResults();
        }

        private void SetIndexesForNationalResults()
        {
            _eligibleVotersIndex = 12;
            _totalVotesIndex = 16;
            _nullVotesIndex = 23;
            _validVotesIndex = 22;
            _sirutaIndex = 5;
            _countryNameIndex = 4;
            _candidatesIndex = 25;
        }

        private void SetIndexesForCorrespondenceResults()
        {
            _eligibleVotersIndex = 12;
            _totalVotesIndex = 13;
            _nullVotesIndex = 15;
            _nullVotesIndex2 = 21;
            _validVotesIndex = 20;
            _sirutaIndex = 5;
            _countryNameIndex = 4;
            _candidatesIndex = 22;
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
                var capitalCityResults = await ImportCapitalCityResults(ballot);
                results.AddRange(capitalCityResults.Candidates);

                electionInfo.ValidVotes += capitalCityResults.ValidVotes;
                electionInfo.NullVotes += capitalCityResults.NullVotes;

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

                var diasporaResults = await AggregateDiasporaResults(query, ballot);
                electionInfo.ValidVotes += diasporaResults.ValidVotes;
                electionInfo.NullVotes += diasporaResults.NullVotes;
                GroupResults(results.Concat(diasporaResults.Candidates).ToList(), electionInfo);
            }

            return electionInfo;
        }

        private static void GroupResults(List<CandidateResult> results, LiveElectionInfo electionInfo)
        {
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

        public async Task<LiveElectionInfo> AggregateDiasporaResults(ElectionResultsQuery query, Ballot ballot)
        {
            var electionInfo = new LiveElectionInfo();

            SetIndexesForNationalResults();
            var diasporaUrl = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.Diaspora, null, null);
            var diasporaResults = await _appCache.GetOrAddAsync(
                $"{diasporaUrl}", () => Import(diasporaUrl.Value),
                DateTimeOffset.Now.AddMinutes(_liveElectionSettings.CsvCacheInMinutes));

            electionInfo.ValidVotes += diasporaResults.Value.ValidVotes;
            electionInfo.NullVotes += diasporaResults.Value.NullVotes;
            SetIndexesForCorrespondenceResults();
            var url = _liveElectionUrlBuilder.GetCorrespondenceUrl(ballot.BallotType, ElectionDivision.Diaspora);
            var correspondenceResults = await _appCache.GetOrAddAsync(
                $"{url}", () => Import(url.Value),
                DateTimeOffset.Now.AddMinutes(_liveElectionSettings.CsvCacheInMinutes));

            electionInfo.ValidVotes += correspondenceResults.Value.ValidVotes;
            electionInfo.NullVotes += correspondenceResults.Value.NullVotes;
            GroupResults(diasporaResults.Value.Candidates.Concat(correspondenceResults.Value.Candidates).ToList(), electionInfo);

            return electionInfo;
        }

        public async Task<LiveElectionInfo> ImportCapitalCityResults(Ballot ballot)
        {
            var electionInfo = new LiveElectionInfo();
            var results = new List<CandidateResult>();
            for (int sectorIndex = 1; sectorIndex <= 6; sectorIndex++)
            {
                var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.County, $"s{sectorIndex}", null);
                var sectorResults = await Import(url.Value);
                if (sectorResults.IsSuccess)
                {
                    results.AddRange(sectorResults.Value.Candidates);
                    electionInfo.ValidVotes += sectorResults.Value.ValidVotes;
                    electionInfo.NullVotes += sectorResults.Value.NullVotes;
                }
            }
            GroupResults(results, electionInfo);

            return electionInfo;
        }

        public async Task<LiveElectionInfo> ImportLocalityResults(Ballot ballot, ElectionResultsQuery query)
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var county = await dbContext.Counties.FirstOrDefaultAsync(c => c.CountyId == query.CountyId);
                if (county == null)
                {
                    return LiveElectionInfo.Default;
                }
                var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.County, county.ShortName, null);
                var stream = await _fileDownloader.Download(url.Value);
                var pollingSections = await ExtractCandidateResultsFromCsv(stream);
                var locality =
                    await dbContext.Localities.FirstOrDefaultAsync(l => l.LocalityId == query.LocalityId);
                var sectionsForLocality = pollingSections.Where(p => p.Siruta == locality.Siruta).ToList();
                LiveElectionInfo electionInfo = new LiveElectionInfo();
                foreach (var pollingSection in sectionsForLocality)
                {
                    electionInfo.ValidVotes += pollingSection.ValidVotes;
                    electionInfo.NullVotes += pollingSection.NullVotes;
                }
                Console.WriteLine($"Added {county.Name}");
                if (locality == null)
                    return LiveElectionInfo.Default;

                var candidateResults = sectionsForLocality.SelectMany(s => s.Candidates).ToList();
                GroupResults(candidateResults, electionInfo);
                return electionInfo;
            }
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
                if (_nullVotesIndex2 != 0)
                    nullVotes += int.Parse(csvParser.GetField(_nullVotesIndex2));
                valid += int.Parse(csvParser.GetField(_validVotesIndex));
                siruta = int.Parse(csvParser.GetField(_sirutaIndex));
                for (int i = _candidatesIndex; i < _candidatesIndex + candidates.Count; i++)
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
        private async Task<List<PollingSection>> ExtractCandidateResultsFromCsv(Stream csvStream)
        {
            List<CandidateResult> candidates;
            List<PollingSection> pollingSections = new List<PollingSection>();
            var csvContent = await ReadCsvContent(csvStream);
            TextReader sr = new StringReader(csvContent);
            var csvParser = new CsvReader(sr, CultureInfo.CurrentCulture);
            csvParser.Configuration.HeaderValidated = null;
            csvParser.Configuration.MissingFieldFound = null;
            candidates = await GetCandidates(csvParser);
            while (true)
            {
                var result = await csvParser.ReadAsync();
                if (!result)
                    return pollingSections;
                var index = 0;
                var pollingSection = new PollingSection
                {
                    EligibleVoters = int.Parse(csvParser.GetField(_eligibleVotersIndex)),
                    Voters = int.Parse(csvParser.GetField(_totalVotesIndex)),
                    NullVotes = int.Parse(csvParser.GetField(_nullVotesIndex)),
                    ValidVotes = int.Parse(csvParser.GetField(_validVotesIndex)),
                    Siruta = int.Parse(csvParser.GetField(_sirutaIndex)),
                    Country = csvParser.GetField(_countryNameIndex),
                    Candidates = JsonConvert.DeserializeObject<List<CandidateResult>>(JsonConvert.SerializeObject(candidates))
                };
                pollingSections.Add(pollingSection);
                for (int i = _candidatesIndex; i < _candidatesIndex + candidates.Count; i++)
                {
                    try
                    {
                        var votes = csvParser.GetField(i);
                        pollingSection.Candidates[index].Votes += int.Parse(votes);
                        index++;
                    }
                    catch (Exception)
                    {
                        return pollingSections;
                    }
                }
            }
        }
        private async Task<List<CandidateResult>> GetCandidates(CsvReader csvParser)
        {
            var readAsync = await csvParser.ReadAsync();
            var candidates = new List<CandidateResult>();
            var index = _candidatesIndex;
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

    internal class PollingSection
    {
        public int EligibleVoters { get; set; }
        public int Voters { get; set; }
        public int NullVotes { get; set; }
        public int ValidVotes { get; set; }
        public int Siruta { get; set; }
        public List<CandidateResult> Candidates { get; set; }
        public string Country { get; set; }
    }
}