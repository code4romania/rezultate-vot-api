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
using CsvHelper.Configuration;
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

    public class CsvTurnoutMap : ClassMap<CsvTurnout>
    {
        public CsvTurnoutMap()
        {
            Map(m => m.County).Name("id");
            Map(m => m.UAT).Name("name");
        }
    }
    public class CsvDownloaderJob : ICsvDownloaderJob
    {
        private readonly IServiceProvider _serviceProvider;
        private HttpClient _httpClient;

        public CsvDownloaderJob(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
            _httpClient = new HttpClient();
        }

        public async Task DownloadFiles()
        {
            var csvUrl = "https://prezenta.roaep.ro/locale27092020/data/csv/simpv/presence_now.csv";
            var stream = await DownloadFile(csvUrl);
            await ProcessStream(stream);
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
                    var counties = await dbContext.Counties.ToListAsync();
                    var localities = await dbContext.Localities.ToListAsync();
                    var csvCounties = turnouts.GroupBy(c => c.County).ToList();
                    var liveElection = await dbContext.Elections.FirstOrDefaultAsync(e => e.Live);
                    var ballots = await dbContext.Ballots.Where(b => b.ElectionId == liveElection.ElectionId).ToListAsync();
                    List<Turnout> dbTurnouts = new List<Turnout>();
                    foreach (var ballot in ballots)
                    {
                        var t = await dbContext.Turnouts.Where(t => t.BallotId == ballot.BallotId).ToListAsync();
                        dbTurnouts.AddRange(t);
                    }

                    Console.WriteLine("Added ballots");
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
                    await dbContext.SaveChangesAsync();
                    Console.WriteLine("Saved vote monitoring");
                    foreach (var csvCounty in csvCounties)
                    {
                        var csvLocalitiesForCounty = csvCounty.GroupBy(c => c.Locality).ToList();
                        var dbCounty = counties.FirstOrDefault(c => c.ShortName.EqualsIgnoringAccent(csvCounty.Key));
                        if (dbCounty == null || dbCounty.CountyId == 16820)
                        {
                            continue;
                        }

                        var countyBallots = ballots.Where(b =>
                            b.BallotType == BallotType.CapitalCityMayor || b.BallotType == BallotType.CountyCouncil ||
                            b.BallotType == BallotType.CountyCouncilPresident).ToList();
                        foreach (var countyBallot in countyBallots)
                        {
                            var dbTurnout = dbTurnouts.FirstOrDefault(t => t.BallotId == countyBallot.BallotId
                                                                           && t.Division == ElectionDivision.County
                                                                           && t.CountyId == dbCounty.CountyId);
                            if (dbTurnout == null)
                                continue;
                            dbTurnout.EligibleVoters = csvCounty.Sum(c => c.EnrolledVoters);
                            dbTurnout.TotalVotes = csvCounty.Sum(c => c.TotalVotes);
                            dbTurnout.PermanentListsVotes = csvCounty.Sum(c => c.LP);
                            dbTurnout.SpecialListsVotes = csvCounty.Sum(c => c.SpecialLists);
                            dbTurnout.ValidVotes = csvCounty.Sum(c => c.TotalVotes);
                        }
                        var localitiesForCounty = localities.Where(l => l.CountyId == dbCounty.CountyId).ToList();
                        foreach (var csvLocality in csvLocalitiesForCounty)
                        {
                            foreach (var ballot in ballots)
                            {
                                var dbLocality = localitiesForCounty.FirstOrDefault(c => c.Name.EqualsIgnoringAccent(csvLocality.Key));
                                if (dbCounty.CountyId == 12913)
                                {
                                    dbLocality = localitiesForCounty.FirstOrDefault(c => c.Name.EqualsIgnoringAccent(csvLocality.Key.Replace("BUCUREŞTI ", "")));
                                }
                                Turnout turnout;
                                if (dbLocality == null)
                                {
                                    dbLocality = new Locality
                                    {
                                        CountyId = dbCounty.CountyId,
                                        Name = csvLocality.Key
                                    };
                                    dbContext.Localities.Add(dbLocality);
                                    await dbContext.SaveChangesAsync();
                                    turnout = new Turnout
                                    {
                                        Division = ElectionDivision.Locality,
                                        CountyId = dbCounty.CountyId,
                                        LocalityId = dbLocality.LocalityId
                                    };
                                }
                                else
                                {
                                    turnout = dbTurnouts.FirstOrDefault(t => t.BallotId == ballot.BallotId
                                                                             && t.Division == ElectionDivision.Locality
                                                                             && t.CountyId == dbCounty.CountyId
                                                                             && t.LocalityId == dbLocality.LocalityId);
                                }
                                if (turnout == null)
                                    continue;

                                turnout.EligibleVoters = csvLocality.Sum(c => c.EnrolledVoters);
                                turnout.TotalVotes = csvLocality.Sum(c => c.TotalVotes);
                                turnout.PermanentListsVotes = csvLocality.Sum(c => c.LP);
                                turnout.SpecialListsVotes = csvLocality.Sum(c => c.SpecialLists);
                                turnout.ValidVotes = csvLocality.Sum(c => c.TotalVotes);
                                turnout.CorrespondenceVotes = csvLocality.Sum(c => c.MobileBallot);
                                dbContext.Turnouts.Update(turnout);
                            }
                        }

                    }

                    foreach (var ballot in ballots)
                    {
                        Console.WriteLine($"Ballot {ballot.BallotId}-{ballot.BallotType}: " +
                                          $"{dbTurnouts.Where(t => t.BallotId == ballot.BallotId).Sum(t => t.EligibleVoters)}");
                    }
                    Console.WriteLine($"Updating {DateTime.Now:F}");
                    await dbContext.BulkUpdateAsync(dbTurnouts);
                }

                Console.WriteLine($"Finished processing at {DateTime.Now:F}");
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
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

    public class CsvTurnout
    {
        [Name("UAT")]
        public string UAT { get; set; }

        [Name("Judet")]
        public string County { get; set; }

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
        public int LP{ get; set; }
    }
}