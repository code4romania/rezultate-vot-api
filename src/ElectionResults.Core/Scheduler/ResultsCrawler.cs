using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using CsvHelper;
using ElectionResults.Core.Configuration;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Query;
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
        }

        public async Task<Result<LiveElectionInfo>> Import(string url, CsvIndexes csvIndexes)
        {
            var electionInfo = await GetCandidatesFromUrl(url, csvIndexes);
            if (electionInfo.Candidates == null)
                return Result.Failure<LiveElectionInfo>("File doesn't exist");
            return electionInfo;
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

                foreach (var countyTurnout in turnouts.Where(t => t.Division == ElectionDivision.County && t.CountyId != Consts.CapitalCity))
                {
                    var county = dbCounties.First(c => c.CountyId == countyTurnout.CountyId);
                    if (county == null)
                    {
                        continue;
                    }
                    var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.County, county.ShortName, null);

                    var countyResults = await Import(url.Value, new CsvIndexes(CsvMode.National));
                    if (countyResults.IsSuccess)
                    {
                        results.AddRange(countyResults.Value.Candidates);
                        electionInfo.ValidVotes += countyResults.Value.ValidVotes;
                        electionInfo.NullVotes += countyResults.Value.NullVotes;
                    }
                }

                var diasporaResults = await AggregateDiasporaResults(query, ballot);
                electionInfo.ValidVotes += diasporaResults.ValidVotes;
                electionInfo.NullVotes += diasporaResults.NullVotes;
                GroupResults(results.Concat(diasporaResults.Candidates).ToList(), electionInfo);
                //PrepareCandidates(electionInfo.Candidates, query, ballot);
                //await UpdateResults(dbContext, electionInfo.Candidates);
            }

            return electionInfo;
        }

        private void PrepareCandidates(List<CandidateResult> electionInfoCandidates, Turnout turnout,
            List<Party> parties)
        {
            foreach (var candidate in electionInfoCandidates)
            {
                candidate.BallotId = turnout.BallotId;
                candidate.Division = turnout.Division;
                candidate.CountryId = turnout.CountryId;
                candidate.CountyId = turnout.CountyId;
                candidate.LocalityId = turnout.LocalityId;
                candidate.PartyId = parties.FirstOrDefault(p => p.Name.EqualsIgnoringAccent(candidate.Name))?.Id ?? parties.FirstOrDefault(p => p.Alias.EqualsIgnoringAccent(candidate.Name))?.Id;
            }
        }

        private void UpdateResults(ApplicationDbContext dbContext, LiveElectionInfo electionInfo, Turnout turnout,
            List<Party> parties)
        {
            PrepareCandidates(electionInfo.Candidates, turnout, parties);

            if (turnout != null)
            {
                turnout.ValidVotes = electionInfo.Candidates.Sum(c => c.Votes);
                turnout.NullVotes = electionInfo.NullVotes;
                dbContext.Update(turnout);
            }
            dbContext.CandidateResults.AddRange(electionInfo.Candidates);
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

            var diasporaUrl = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.Diaspora, null, null);
            var diasporaResults = await _appCache.GetOrAddAsync(
                $"{diasporaUrl}", () => Import(diasporaUrl.Value, new CsvIndexes(CsvMode.Diaspora)),
                DateTimeOffset.Now.AddMinutes(_liveElectionSettings.CsvCacheInMinutes));

            electionInfo.ValidVotes += diasporaResults.Value.ValidVotes;
            electionInfo.NullVotes += diasporaResults.Value.NullVotes;
            var url = _liveElectionUrlBuilder.GetCorrespondenceUrl(ballot.BallotType, ElectionDivision.Diaspora);
            var correspondenceResults = await _appCache.GetOrAddAsync(
                $"{url}", () => Import(url.Value, new CsvIndexes(CsvMode.Correspondence)),
                DateTimeOffset.Now.AddMinutes(_liveElectionSettings.CsvCacheInMinutes));

            electionInfo.ValidVotes += correspondenceResults.Value.ValidVotes;
            electionInfo.NullVotes += correspondenceResults.Value.NullVotes;
            GroupResults(diasporaResults.Value.Candidates.Concat(correspondenceResults.Value.Candidates).ToList(), electionInfo);

            return electionInfo;
        }

        public async Task<LiveElectionInfo> ImportCapitalCityResults(Ballot ballot, int? sector = null)
        {
            var electionInfo = new LiveElectionInfo();
            var results = new List<CandidateResult>();
            if (sector != null)
            {
                await GetResultsForSector(ballot, sector.Value, results, electionInfo);
            }
            else
            {
                for (int sectorIndex = 1; sectorIndex <= 6; sectorIndex++)
                {
                    await GetResultsForSector(ballot, sectorIndex, results, electionInfo);
                }
            }
            GroupResults(results, electionInfo);

            return electionInfo;
        }

        private async Task GetResultsForSector(Ballot ballot, int sectorIndex, List<CandidateResult> results, LiveElectionInfo electionInfo)
        {
            var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.County, $"s{sectorIndex}", null);
            var sectorResults = await Import(url.Value, new CsvIndexes(CsvMode.National));
            if (sectorResults.IsSuccess)
            {
                results.AddRange(sectorResults.Value.Candidates);
                electionInfo.ValidVotes += sectorResults.Value.ValidVotes;
                electionInfo.NullVotes += sectorResults.Value.NullVotes;
            }
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
                var stream = await _appCache.GetOrAddAsync(
                    $"{url}", () => _fileDownloader.Download(url.Value));
                var pollingSections = await ExtractCandidateResultsFromCsv(stream, new CsvIndexes(CsvMode.National));
                var locality =
                    await dbContext.Localities.FirstOrDefaultAsync(l => l.LocalityId == query.LocalityId);
                var sectionsForLocality = pollingSections.Where(p => p.Siruta == locality.Siruta).ToList();
                LiveElectionInfo electionInfo = new LiveElectionInfo();
                foreach (var pollingSection in sectionsForLocality)
                {
                    electionInfo.ValidVotes += pollingSection.ValidVotes;
                    electionInfo.NullVotes += pollingSection.NullVotes;
                }
                if (locality == null)
                    return LiveElectionInfo.Default;

                var candidateResults = sectionsForLocality.SelectMany(s => s.Candidates).ToList();
                GroupResults(candidateResults, electionInfo);

                return electionInfo;
            }
        }

        public async Task<LiveElectionInfo> ImportCountryResults(ElectionResultsQuery query, Ballot ballot)
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var country = await dbContext.Countries.FirstOrDefaultAsync(c => c.Id == query.CountryId);
                if (country == null)
                {
                    return LiveElectionInfo.Default;
                }
                LiveElectionInfo electionInfo = new LiveElectionInfo();
                var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.Diaspora, null, null);
                var diasporaResults = await GetDiasporaResults(url, country, electionInfo, new CsvIndexes(CsvMode.Diaspora));
                var correspondenceUrl = _liveElectionUrlBuilder.GetCorrespondenceUrl(ballot.BallotType, ElectionDivision.Diaspora);
                var correspondenceResults = await GetDiasporaResults(correspondenceUrl, country, electionInfo, new CsvIndexes(CsvMode.Correspondence));
                GroupResults(diasporaResults.Concat(correspondenceResults).ToList(), electionInfo);
                return electionInfo;
            }
        }

        public async Task ImportAll()
        {
            using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
            {
                var parties = await dbContext.Parties.ToListAsync();
                var election = await dbContext.Elections.FirstOrDefaultAsync(e => e.Live);
                if(election == null)
                    return;
                var ballots = await dbContext.Ballots.Where(b => b.Election.Live).ToListAsync();
                foreach (var ballot in ballots)
                {
                    Console.WriteLine($"Importing {ballot.BallotType} results");
                    var allTurnouts = await dbContext.Turnouts
                        .Where(t => t.BallotId == ballot.BallotId)
                        .ToListAsync();
                    var nationalResults = await AggregateNationalResults(new ElectionResultsQuery
                    {
                        BallotId = ballot.BallotId,
                        Division = ElectionDivision.National
                    }, ballot);
                    var allLocalities = await dbContext.Localities.ToListAsync();
                    var allCounties = await dbContext.Counties.ToListAsync();
                    await ImportDiasporaFinalResults(ballot, allTurnouts, dbContext, allLocalities, parties);
                    var pollingSectionsByCounty = await ImportCounties(allTurnouts, ballot, allCounties, dbContext, parties);
                    ImportLocalities(allCounties, allLocalities, allTurnouts, pollingSectionsByCounty, ballot, dbContext, parties);
                    await ImportCapitalCityFinalResults(ballot, allTurnouts, dbContext, allLocalities, parties);
                    
                    var nationalTurnout = allTurnouts.FirstOrDefault(t => t.Division == ElectionDivision.National && t.BallotId == ballot.BallotId);
                    UpdateResults(dbContext, nationalResults, nationalTurnout, parties);
                }

                election.Live = false;
                Console.WriteLine($"Updating the database");
                await dbContext.SaveChangesAsync();
                Console.WriteLine("Import finished");
            }
        }

        private async Task ImportDiasporaFinalResults(Ballot ballot, List<Turnout> allTurnouts,
            ApplicationDbContext dbContext, List<Locality> allLocalities, List<Party> parties)
        {
            var allCountries = await dbContext.Countries.ToListAsync();
            var diasporaElectionInfo = new LiveElectionInfo { Candidates = new List<CandidateResult>() };
            
            foreach (var turnout in allTurnouts.Where(t => t.Division == ElectionDivision.Diaspora_Country))
            {
                var country = allCountries.FirstOrDefault(c => c.Id == turnout.CountryId);
                var electionInfo = await ImportCountryResults(new ElectionResultsQuery
                {
                    CountryId = country.Id,
                    Division = ElectionDivision.Diaspora_Country,
                    BallotId = ballot.BallotId
                }, ballot);
                diasporaElectionInfo.Candidates.AddRange(JsonConvert.DeserializeObject<List<CandidateResult>>(JsonConvert.SerializeObject(electionInfo.Candidates)));
                UpdateResults(dbContext, electionInfo, turnout, parties);
                diasporaElectionInfo.ValidVotes += electionInfo.ValidVotes;
                diasporaElectionInfo.TotalVotes += turnout.TotalVotes;
                diasporaElectionInfo.NullVotes += electionInfo.NullVotes;
                
            }

            GroupResults(diasporaElectionInfo.Candidates, diasporaElectionInfo);
            UpdateResults(dbContext, diasporaElectionInfo, allTurnouts.FirstOrDefault(d => d.Division == ElectionDivision.Diaspora && d.BallotId == ballot.BallotId), parties);
        }

        private async Task ImportCapitalCityFinalResults(Ballot ballot, List<Turnout> allTurnouts,
            ApplicationDbContext dbContext,
            List<Locality> allLocalities, List<Party> parties)
        {
            try
            {
                var sectors = allLocalities.Where(l => l.CountyId == Consts.CapitalCity);
                var capitalCityResults = new LiveElectionInfo { Candidates = new List<CandidateResult>() };
                foreach (var sector in sectors.Where(s => s.Name.ToLower().StartsWith("toate") == false))
                {
                    var index = int.Parse(sector.Name.Split(" ").Last());

                    var sectorResults = await ImportCapitalCityResults(ballot, index);
                    var sectorTurnout =
                        allTurnouts.FirstOrDefault(t => t.LocalityId == sector.LocalityId && t.BallotId == ballot.BallotId);
                    capitalCityResults.Candidates.AddRange(sectorResults.Candidates);
                    capitalCityResults.TotalVotes += sectorTurnout.TotalVotes;
                    capitalCityResults.ValidVotes += sectorResults.ValidVotes;
                    capitalCityResults.NullVotes += sectorResults.NullVotes;
                    UpdateResults(dbContext, sectorResults, sectorTurnout, parties);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

        }

        private void ImportLocalities(List<County> allCounties, List<Locality> allLocalities, List<Turnout> allTurnouts,
            List<PollingSection> pollingSectionsByCounty,
            Ballot ballot, ApplicationDbContext dbContext, List<Party> parties)
        {
            LiveElectionInfo localitiesElectionInfo = new LiveElectionInfo { Candidates = new List<CandidateResult>() };
            foreach (var county in allCounties.Where(c => c.CountyId != Consts.CapitalCity))
            {
                foreach (var locality in allLocalities.Where(l =>
                    l.CountyId == county.CountyId && allTurnouts.Any(t => t.LocalityId == l.LocalityId)))
                {
                    var sectionsForLocality = pollingSectionsByCounty.Where(p => p.Siruta == locality.Siruta).ToList();
                    LiveElectionInfo electionInfo = new LiveElectionInfo();
                    foreach (var pollingSection in sectionsForLocality)
                    {
                        electionInfo.ValidVotes += pollingSection.ValidVotes;
                        electionInfo.NullVotes += pollingSection.NullVotes;
                    }

                    var candidateResults = sectionsForLocality.SelectMany(s => s.Candidates).ToList();
                    localitiesElectionInfo.Candidates.AddRange(JsonConvert.DeserializeObject<List<CandidateResult>>(JsonConvert.SerializeObject(candidateResults)));
                    GroupResults(candidateResults, electionInfo);
                    var turnout =
                        allTurnouts.FirstOrDefault(t => t.LocalityId == locality.LocalityId && t.BallotId == ballot.BallotId);
                    UpdateResults(dbContext, electionInfo, turnout, parties);
                    localitiesElectionInfo.ValidVotes += electionInfo.ValidVotes;
                    localitiesElectionInfo.TotalVotes += electionInfo.TotalVotes;
                    localitiesElectionInfo.NullVotes += electionInfo.NullVotes;
                }
            }
        }

        private async Task<List<PollingSection>> ImportCounties(List<Turnout> allTurnouts, Ballot ballot,
            List<County> allCounties, ApplicationDbContext dbContext, List<Party> parties)
        {
            var countyTurnouts = allTurnouts.Where(t =>
                t.Division == ElectionDivision.County && t.BallotId == ballot.BallotId).ToList();
            var pollingSectionsByCounty = new List<PollingSection>();
            foreach (var countyTurnout in countyTurnouts.Where(c => c.CountyId != Consts.CapitalCity))
            {
                var county = allCounties.FirstOrDefault(c => c.CountyId == countyTurnout.CountyId);
                var url = _liveElectionUrlBuilder.GetFileUrl(ballot.BallotType, ElectionDivision.County, county.ShortName,
                    null);
                var stream = await _appCache.GetOrAddAsync(
                    $"{url}", () => _fileDownloader.Download(url.Value));
                var pollingSections = await ExtractCandidateResultsFromCsv(stream, new CsvIndexes(CsvMode.National));
                pollingSectionsByCounty.AddRange(pollingSections);
                stream.Seek(0, SeekOrigin.Begin);
                var countyElectionInfo = await ExtractCandidatesFromCsv(stream, new CsvIndexes(CsvMode.National));
                var turnout = allTurnouts.FirstOrDefault(t =>
                    t.CountyId == county.CountyId && t.BallotId == ballot.BallotId && t.Division == ElectionDivision.County);
                UpdateResults(dbContext, countyElectionInfo, turnout, parties);
            }

            return pollingSectionsByCounty;
        }

        private async Task<List<CandidateResult>> GetDiasporaResults(Result<string> url, Country country,
            LiveElectionInfo electionInfo, CsvIndexes csvIndexes)
        {
            var stream = await _fileDownloader.Download(url.Value);
            var pollingSections = await ExtractCandidateResultsFromCsv(stream, csvIndexes);
            var sectionsForLocality = pollingSections.Where(p => p.Country.NormalizeCountryName().EqualsIgnoringAccent(country.Name)).ToList();
            foreach (var pollingSection in sectionsForLocality)
            {
                electionInfo.ValidVotes += pollingSection.ValidVotes;
                electionInfo.NullVotes += pollingSection.NullVotes;
            }

            return sectionsForLocality.SelectMany(s => s.Candidates).ToList();
        }

        public async Task<LiveElectionInfo> GetCandidatesFromUrl(string url, CsvIndexes csvIndexes)
        {
            try
            {
                var stream = await _fileDownloader.Download(url);
                var liveElectionInfo = await ExtractCandidatesFromCsv(stream, csvIndexes);
                return liveElectionInfo;
            }
            catch
            {
                return LiveElectionInfo.Default;
            }
        }

        private async Task<LiveElectionInfo> ExtractCandidatesFromCsv(Stream csvStream, CsvIndexes csvIndexes)
        {
            List<CandidateResult> candidates;
            var csvContent = await ReadCsvContent(csvStream);
            TextReader sr = new StringReader(csvContent);
            var csvParser = new CsvReader(sr, CultureInfo.CurrentCulture);
            csvParser.Configuration.HeaderValidated = null;
            csvParser.Configuration.MissingFieldFound = null;
            candidates = await GetCandidates(csvParser, csvIndexes);
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
                total += int.Parse(csvParser.GetField(csvIndexes.EligibleVotersIndex));
                voted += int.Parse(csvParser.GetField(csvIndexes.TotalVotesIndex));
                nullVotes += int.Parse(csvParser.GetField(csvIndexes.NullVotesIndex));
                if (csvIndexes.NullVotesIndex2 != 0)
                    nullVotes += int.Parse(csvParser.GetField(csvIndexes.NullVotesIndex2));
                valid += int.Parse(csvParser.GetField(csvIndexes.ValidVotesIndex));
                siruta = int.Parse(csvParser.GetField(csvIndexes.SirutaIndex));
                for (int i = csvIndexes.CandidatesIndex; i < csvIndexes.CandidatesIndex + candidates.Count; i++)
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

        private async Task<List<PollingSection>> ExtractCandidateResultsFromCsv(Stream csvStream, CsvIndexes csvIndexes)
        {
            List<CandidateResult> candidates;
            List<PollingSection> pollingSections = new List<PollingSection>();
            var csvContent = await ReadCsvContent(csvStream);
            TextReader sr = new StringReader(csvContent);
            var csvParser = new CsvReader(sr, CultureInfo.CurrentCulture);
            csvParser.Configuration.HeaderValidated = null;
            csvParser.Configuration.MissingFieldFound = null;
            candidates = await GetCandidates(csvParser, csvIndexes);
            while (true)
            {
                var result = await csvParser.ReadAsync();
                if (!result)
                    return pollingSections;
                var index = 0;
                var pollingSection = new PollingSection
                {
                    EligibleVoters = int.Parse(csvParser.GetField(csvIndexes.EligibleVotersIndex)),
                    Voters = int.Parse(csvParser.GetField(csvIndexes.TotalVotesIndex)),
                    NullVotes = int.Parse(csvParser.GetField(csvIndexes.NullVotesIndex)),
                    ValidVotes = int.Parse(csvParser.GetField(csvIndexes.ValidVotesIndex)),
                    Siruta = int.Parse(csvParser.GetField(csvIndexes.SirutaIndex)),
                    Country = csvParser.GetField(csvIndexes.CountryNameIndex),
                    Candidates = JsonConvert.DeserializeObject<List<CandidateResult>>(JsonConvert.SerializeObject(candidates))
                };
                if (csvIndexes.NullVotesIndex2 != 0)
                {
                    pollingSection.NullVotes += int.Parse(csvParser.GetField(csvIndexes.NullVotesIndex2));
                }

                pollingSections.Add(pollingSection);
                for (int i = csvIndexes.CandidatesIndex; i < csvIndexes.CandidatesIndex + candidates.Count; i++)
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

        private async Task<List<CandidateResult>> GetCandidates(CsvReader csvParser, CsvIndexes csvIndexes)
        {
            await csvParser.ReadAsync();
            var candidates = new List<CandidateResult>();
            var index = csvIndexes.CandidatesIndex;
            await csvIndexes.Map(csvParser);
            while (true)
            {
                try
                {
                    var field = csvParser.GetField(index++);
                    if (field == null)
                        return candidates;
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