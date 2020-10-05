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
using CsvHelper.Configuration.Attributes;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Z.EntityFramework.Extensions;

namespace ElectionResults.Core.Scheduler
{
    public class CsvDownloaderJob : ICsvDownloaderJob
    {
        private readonly IServiceProvider _serviceProvider;
        private HttpClient _httpClient;
        private List<Turnout> _turnouts;
        private List<CandidateResult> _candidates;

        public CsvDownloaderJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _httpClient = new HttpClient();
        }

        public async Task<LiveElectionInfo> GetCandidatesFromUrl(string url)
        {
            var stream = await DownloadFile(url);
            var liveElectionInfo = await ExtractCandidatesFromCsv(stream);
            return liveElectionInfo;
        }

        public async Task DownloadFiles()
        {
            await DownloadCandidates();
            var csvUrl = "https://prezenta.roaep.ro/locale27092020/data/csv/simpv/presence_now.csv";
            var stream = await DownloadFile(csvUrl);
            await ProcessStream(stream);
        }

        private async Task DownloadCandidates()
        {
            try
            {
                using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
                {
                    var ballots = await dbContext.Ballots.Where(b => b.Election.Live).ToListAsync();
                    if (ballots.Any() == false)
                        return;
                    EntityFrameworkManager.ContextFactory = context => dbContext;
                    var counties = await dbContext.Counties.Include(c => c.Localities).Where(c => c.Name != "Diaspora").ToListAsync();
                    var parties = await dbContext.Parties.Where(p => p.Name.Length > 5).ToListAsync();
                    _turnouts = new List<Turnout>();
                    _candidates = new List<CandidateResult>();
                    foreach (var ballot in ballots)
                    {
                        Console.WriteLine($"Inserting {ballot.BallotType} results");
                        var winners = await dbContext.Winners.Where(w => w.BallotId == ballot.BallotId).ToListAsync();
                        var resultsForBallot =
                            await dbContext.CandidateResults.Where(c => c.BallotId == ballot.BallotId).ToListAsync();
                        await dbContext.Winners.BulkDeleteAsync(winners);
                        await dbContext.CandidateResults.BulkDeleteAsync(resultsForBallot);
                        await dbContext.Turnouts.BulkDeleteAsync(
                            dbContext.Turnouts.Where(t => t.BallotId == ballot.BallotId));
                        await AddResults(counties, dbContext, ballot, parties);
                    }

                    await dbContext.CandidateResults.BulkInsertAsync(_candidates);
                    await dbContext.Turnouts.BulkInsertAsync(_turnouts);
                    var liveElection = await dbContext.Elections.FirstOrDefaultAsync(e => e.Live);
                    if (liveElection != null)
                    {
                        liveElection.Live = false;
                        dbContext.Elections.Update(liveElection);
                        await dbContext.SaveChangesAsync();
                    }
                    Console.WriteLine("Finished");
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task AddResults(List<County> counties, ApplicationDbContext dbContext, Ballot ballot,
            List<Party> parties)
        {
            foreach (var county in counties)
            {
                var data = await GetCountyResults(county);
                var localities = county.Localities.Where(l => l.Siruta > 0).ToList();
                List<JsonCandidateModel> list = new List<JsonCandidateModel>();
                if (ballot.BallotType == BallotType.LocalCouncil)
                    list = data.Stages.FINAL.scopes.UAT.Categories.CL.Table.OrderBy(c => c.uat_name).ToList();
                else if (ballot.BallotType == BallotType.Mayor)
                    list = data.Stages.FINAL.scopes.UAT.Categories.P.Table.OrderBy(c => c.uat_name).ToList();
                else if (ballot.BallotType == BallotType.CountyCouncil)
                    list = data.Stages.FINAL.scopes.CNTY.Categories.CJ.Table.OrderBy(c => c.uat_name).ToList();
                else if (ballot.BallotType == BallotType.CountyCouncilPresident)
                    list = data.Stages.FINAL.scopes.CNTY.Categories.PCJ.Table.OrderBy(c => c.uat_name).ToList();

                if ((ballot.BallotType == BallotType.CountyCouncilPresident ||
                    ballot.BallotType == BallotType.CountyCouncil) && list.Any())
                {
                    var jsonCounty = list.FirstOrDefault(l => l.county_code.ToLower() == county.ShortName.ToLower());
                    UpdateCountyCandidates(jsonCounty, county, ballot, parties);
                }

                if (ballot.BallotType == BallotType.Mayor || ballot.BallotType == BallotType.LocalCouncil)
                {
                    foreach (var jsonLocality in list)
                    {
                        await UpdateLocalityCandidates(localities, jsonLocality, dbContext, county, ballot,
                            parties);
                    }
                }
                Console.WriteLine($"Finished county {county.Name}");
            }
        }

        private void UpdateCountyCandidates(JsonCandidateModel jsonLocality, County county, Ballot ballot, List<Party> parties)
        {
            var newResults = jsonLocality.Votes.Select(r => CreateCandidateResult(r, ballot, parties, null, county.CountyId, ElectionDivision.County))
                .ToList();
            var turnout = GetTurnout(jsonLocality.Fields);
            turnout.Division = ElectionDivision.County;
            turnout.BallotId = ballot.BallotId;
            turnout.CountyId = county.CountyId;
            _turnouts.Add(turnout);
            _candidates.AddRange(newResults);
        }

        private async Task UpdateLocalityCandidates(List<Locality> localities, JsonCandidateModel jsonLocality,
            ApplicationDbContext dbContext, County county, Ballot ballot, List<Party> parties)
        {
            var locality =
                localities.FirstOrDefault(l => l.Siruta == jsonLocality.uat_siruta);
            if (locality == null)
            {
                Console.WriteLine($"Siruta not found for {jsonLocality.uat_name} - {jsonLocality.uat_siruta}");
                locality = await UpdateSirutaForLocality(jsonLocality, dbContext, county.CountyId);
            }
            var turnout = GetTurnout(jsonLocality.Fields);
            turnout.Division = ElectionDivision.Locality;
            turnout.BallotId = ballot.BallotId;
            turnout.LocalityId = locality.LocalityId;
            turnout.CountyId = county.CountyId;
            _turnouts.Add(turnout);
            var newResults = jsonLocality.Votes.Select(r => CreateCandidateResult(r, ballot, parties, locality.LocalityId, locality.CountyId))
                .ToList();
            _candidates.AddRange(newResults);
        }

        private static Turnout GetTurnout(Field[] fields)
        {
            var turnout = new Turnout();
            turnout.EligibleVoters = fields.FirstOrDefault(f => f.Name == "a")?.Value ?? 0;
            turnout.ValidVotes = fields.FirstOrDefault(f => f.Name == "c")?.Value ?? 0;
            turnout.NullVotes = fields.FirstOrDefault(f => f.Name == "d")?.Value ?? 0;
            turnout.PermanentListsVotes = fields.FirstOrDefault(f => f.Name == "b1")?.Value ?? 0;
            turnout.SuplimentaryVotes = fields.FirstOrDefault(f => f.Name == "b3")?.Value ?? 0;
            turnout.VotesByMail = fields.FirstOrDefault(f => f.Name == "b4")?.Value ?? 0;
            turnout.TotalVotes = turnout.ValidVotes + turnout.NullVotes;
            return turnout;
        }

        private static async Task<Locality> UpdateSirutaForLocality(
            JsonCandidateModel jsonLocality,
            ApplicationDbContext dbContext, int countyId)
        {
            Locality locality;
            locality = new Locality
            {
                Name = jsonLocality.uat_name,
                CountyId = countyId,
                Siruta = jsonLocality.uat_siruta.GetValueOrDefault()
            };
            dbContext.Localities.Update(locality);
            await dbContext.SaveChangesAsync();
            return locality;
        }

        private static CandidateResult CreateCandidateResult(Vote vote, Ballot ballot, List<Party> parties,
            int? localityId, int? countyId, ElectionDivision division = ElectionDivision.Locality)
        {
            var candidateResult = new CandidateResult
            {
                BallotId = ballot.BallotId,
                Division = division,
                Votes = vote.Votes,
                Name = vote.Candidate,
                CountyId = countyId,
                LocalityId = localityId,
                Seats1 = vote.Mandates1,
                Seats2 = vote.Mandates2
            };
            var partyName = ballot.BallotType == BallotType.LocalCouncil || ballot.BallotType == BallotType.CountyCouncil ? vote.Candidate : vote.Party;
            if (partyName.IsNotEmpty())
                candidateResult.PartyId = parties.FirstOrDefault(p => p.Alias.ContainsString(partyName))?.Id
                                          ?? parties.FirstOrDefault(p => p.Name.ContainsString(partyName))?.Id;
            candidateResult.PartyName = partyName;
            if (candidateResult.PartyId == null && partyName.IsNotEmpty())
            {
                candidateResult.ShortName = partyName.GetPartyShortName(null);
            }

            return candidateResult;
        }
        private Dictionary<int, JsonResultsModel> _countyResults = new Dictionary<int, JsonResultsModel>();
        private async Task<JsonResultsModel> GetCountyResults(County county)
        {
            if (_countyResults.ContainsKey(county.CountyId))
                return _countyResults[county.CountyId];
            var httpClientHandler = new HttpClientHandler
            {
                AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
            };
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
            _httpClient = new HttpClient(httpClientHandler);
            var response = await _httpClient.GetStringAsync(
                $"https://prezenta.roaep.ro/locale27092020/data/json/sicpv/pv/pv_{county.ShortName.ToLower()}_final.json?_=1701776878922");
            var data = JsonConvert.DeserializeObject<JsonResultsModel>(response);
            _countyResults[county.CountyId] = data;
            return data;
        }

        private async Task<Stream> DownloadFile(string url)
        {
            try
            {
                var httpClientHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate
                };
                httpClientHandler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
                _httpClient = new HttpClient(httpClientHandler);
                var response = await _httpClient.GetStringAsync(url);
                return new MemoryStream(Encoding.UTF8.GetBytes(response));
            }
            catch (Exception e)
            {
                Log.LogError(e, $"Failed to download file: {url}");
                throw;
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
                for (int i = 26; i < candidates.Count + 26; i++)
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

        private async Task ProcessStream(Stream csvStream)
        {
            try
            {
                Console.WriteLine($"Started at {DateTime.Now:F}");
                var csvContent = await ReadCsvContent(csvStream);
                TextReader sr = new StringReader(csvContent);
                var csvParser = new CsvReader(sr, CultureInfo.CurrentCulture);
                csvParser.Configuration.HeaderValidated = null;
                csvParser.Configuration.MissingFieldFound = null;
                var turnouts = csvParser.GetRecords<CsvTurnout>().ToList();
                using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
                {
                    EntityFrameworkManager.ContextFactory = context => dbContext;
                    var localities = await dbContext.Localities.ToListAsync();
                    var csvLocalities = turnouts.GroupBy(c => c.Siruta).ToList();
                    var liveElection = await dbContext.Elections.FirstOrDefaultAsync(e => e.Live);
                    if (liveElection == null)
                        return;
                    var ballots = await dbContext.Ballots.Where(b => b.ElectionId == liveElection.ElectionId).ToListAsync();
                    List<Turnout> dbTurnouts = new List<Turnout>();
                    foreach (var ballot in ballots)
                    {
                        var t = await dbContext.Turnouts.Where(t => t.BallotId == ballot.BallotId).ToListAsync();
                        dbTurnouts.AddRange(t);
                    }

                    foreach (var csvLocality in csvLocalities)
                    {
                        var dbLocality = localities.FirstOrDefault(l => l.Siruta == csvLocality.Key);
                        if (dbLocality == null)
                            continue;
                        foreach (var ballot in ballots)
                        {
                            var turnout = dbTurnouts.FirstOrDefault(t => t.BallotId == ballot.BallotId
                                                                     && t.Division == ElectionDivision.Locality
                                                                     && t.CountyId == dbLocality.CountyId
                                                                     && t.LocalityId == dbLocality.LocalityId);

                            if (turnout == null)
                            {
                                continue;
                            }

                            turnout.EligibleVoters = csvLocality.Sum(c => c.EnrolledVoters + c.ComplementaryList);
                            turnout.TotalVotes = csvLocality.Sum(c => c.TotalVotes);
                            turnout.PermanentListsVotes = csvLocality.Sum(c => c.LP);
                            turnout.SpecialListsVotes = csvLocality.Sum(c => c.SpecialLists);
                            turnout.ValidVotes = csvLocality.Sum(c => c.TotalVotes);
                            turnout.CorrespondenceVotes = csvLocality.Sum(c => c.MobileBallot);
                        }
                    }
                    var countyTurnout = dbTurnouts.FirstOrDefault(t => t.Division == ElectionDivision.National);
                    if (countyTurnout != null)
                    {
                        countyTurnout.EligibleVoters = turnouts.Sum(t => t.EnrolledVoters + t.ComplementaryList);
                        countyTurnout.TotalVotes = turnouts.Sum(t => t.TotalVotes);
                        countyTurnout.ValidVotes = countyTurnout.TotalVotes;
                        dbContext.Update(countyTurnout);
                        await dbContext.SaveChangesAsync();
                    }

                    await dbContext.SaveChangesAsync();
                    Console.WriteLine("Done");
                }
                Console.WriteLine($"Finished processing at {DateTime.Now:F}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Finished processing at {DateTime.Now:F}");
                Console.WriteLine(e);
            }
        }

        private async Task UpdateObservation(List<Ballot> ballots, ApplicationDbContext dbContext)
        {
            var voteMonitoringStats = await GetVoteMonitoringStats();
            foreach (var ballot in ballots)
            {
                var statistics = voteMonitoringStats.Value.Statistics;
                var observation = await dbContext.Observations.FirstOrDefaultAsync(o => o.BallotId == ballot.BallotId);
                if (observation == null)
                {
                    dbContext.Observations.Add(new Observation
                    {
                        BallotId = ballot.BallotId,
                        MessageCount = int.Parse(statistics[0].Value),
                        CoveredPollingPlaces = int.Parse(statistics[1].Value),
                        CoveredCounties = int.Parse(statistics[2].Value),
                        IssueCount = int.Parse(statistics[5].Value),
                        ObserverCount = int.Parse(statistics[4].Value)
                    });
                }
                else
                {
                    observation.BallotId = ballot.BallotId;
                    observation.MessageCount = int.Parse(statistics[0].Value);
                    observation.CoveredPollingPlaces = int.Parse(statistics[1].Value);
                    observation.CoveredCounties = int.Parse(statistics[2].Value);
                    observation.IssueCount = int.Parse(statistics[3].Value);
                    observation.ObserverCount = int.Parse(statistics[4].Value);
                    dbContext.Observations.Update(observation);
                }
            }
        }

        private static Turnout CreateTurnout(County dbCounty, Locality dbLocality, Ballot ballot)
        {
            return new Turnout
            {
                Division = ElectionDivision.Locality,
                CountyId = dbCounty.CountyId,
                LocalityId = dbLocality.LocalityId,
                BallotId = ballot.BallotId
            };
        }

        protected virtual async Task<string> ReadCsvContent(Stream csvStream)
        {
            var buffer = new byte[csvStream.Length];
            await csvStream.ReadAsync(buffer, 0, (int)csvStream.Length);
            var csvContent = Encoding.UTF8.GetString(buffer);
            return csvContent;
        }


        public async Task<Result<VoteMonitoringStats>> GetVoteMonitoringStats()
        {
            var httpClient = new HttpClient();
            var json = await httpClient.GetStringAsync("https://app-vmon-api-dev.azurewebsites.net/api/v1/statistics/mini/all");
            var response = JsonConvert.DeserializeObject<List<MonitoringInfo>>(json);
            return Result.Success(new VoteMonitoringStats
            {
                Statistics = response,
            });
        }
    }

    public class LiveElectionInfo
    {
        public List<CandidateResult> Candidates { get; set; }

        public int EligibleVoters { get; set; }

        public int NullVotes { get; set; }

        public int TotalVotes { get; set; }
        public int ValidVotes { get; set; }

        public static LiveElectionInfo Default { get; } = new LiveElectionInfo();
    }

    public class CsvTurnout
    {
        [Name("UAT")]
        public string UAT { get; set; }

        [Name("Judet")]
        public string County { get; set; }

        [Name("Siruta")]
        public int Siruta { get; set; }

        [Name("Localitate")]
        public string Locality { get; set; }

        [Name("Localitate")]
        public string Section { get; set; }

        [Name("Votanti pe lista permanenta")]
        public int EnrolledVoters { get; set; }

        [Name("Votanti pe lista complementara")]
        public int ComplementaryList { get; set; }

        [Name("LT")]
        public int TotalVotes { get; set; }

        [Name("UM")]
        public int MobileBallot { get; set; }

        [Name("LS")]
        public int SpecialLists { get; set; }

        [Name("LP")]
        public int LP { get; set; }
    }
}