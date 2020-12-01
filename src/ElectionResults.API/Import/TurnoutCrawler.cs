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
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Infrastructure;
using ElectionResults.Core.Repositories;
using ElectionResults.Core.Scheduler;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Z.EntityFramework.Extensions;

namespace ElectionResults.API.Import
{
    public class TurnoutCrawler : ITurnoutCrawler
    {
        private readonly IServiceProvider _serviceProvider;
        private static HttpClient _httpClient;
        private List<County> _dbCounties;
        private List<Country> _dbCountries;
        private List<Locality> _localities;
        private LiveElectionSettings _settings;

        public TurnoutCrawler(IServiceProvider serviceProvider, IOptions<LiveElectionSettings> options)
        {
            _serviceProvider = serviceProvider;
            _httpClient = new HttpClient();
            _settings = options.Value;
        }

        public async Task Import()
        {
            try
            {
                var stream = await DownloadFile("https://prezenta.roaep.ro/presa/prezenta/PRL/presence_now.csv");
                await ProcessStream(stream);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private async Task<Stream> DownloadFile(string url)
        {
            try
            {
                var httpClientHandler = new HttpClientHandler
                {
                    AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                    Credentials = new NetworkCredential(_settings.FtpUser, _settings.FtpPassword)
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
                Console.WriteLine($"Started downloading turnout at {DateTime.Now:F}");
                var csvContent = await ReadCsvContent(csvStream);
                TextReader sr = new StringReader(csvContent);
                var csvParser = new CsvReader(sr, CultureInfo.CurrentCulture);
                csvParser.Configuration.HeaderValidated = null;
                csvParser.Configuration.MissingFieldFound = null;
                var turnouts = csvParser.GetRecords<CsvTurnout>().ToList();
                using (var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>())
                {
                    EntityFrameworkManager.ContextFactory = context => dbContext;
                    _dbCounties = await dbContext.Counties.ToListAsync();
                    _dbCountries = await dbContext.Countries.ToListAsync();
                    _localities = await dbContext.Localities.ToListAsync();
                    var liveElection = await dbContext.Elections.FirstOrDefaultAsync(e => e.Live);
                    if (liveElection == null)
                        return;
                    var ballots = await dbContext.Ballots.Where(b => b.ElectionId == liveElection.ElectionId).ToListAsync();
                    await UpdateObservation(ballots, dbContext);

                    List<Turnout> turnoutsForElection = new List<Turnout>();
                    foreach (var ballot in ballots)
                    {
                        var turnoutsForBallot = await dbContext.Turnouts.Where(t => t.BallotId == ballot.BallotId).ToListAsync();
                        turnoutsForElection.AddRange(turnoutsForBallot);
                    }

                    var counties = turnouts.GroupBy(t => t.County).ToList();
                    foreach (var ballot in ballots)
                    {
                        var turnoutsForBallot = await dbContext.Turnouts.Where(t => t.BallotId == ballot.BallotId).ToListAsync();
                        UpdateDiaspora(ballot, counties.FirstOrDefault(c => c.Key == "SR"), dbContext, turnoutsForBallot);
                        UpdateCountiesAndLocalities(counties, ballot, dbContext, turnoutsForBallot);
                        UpdateNationalTurnout(ballot, dbContext, turnoutsForBallot, turnouts);
                    }

                    await dbContext.SaveChangesAsync();
                    Console.WriteLine("Done");
                }
                Console.WriteLine($"Finished downloading turnout at {DateTime.Now:F}");
            }
            catch (Exception e)
            {
                Log.LogError(e);
            }
        }

        private void UpdateCountiesAndLocalities(List<IGrouping<string, CsvTurnout>> counties, Ballot ballot, ApplicationDbContext dbContext, List<Turnout> turnoutsForBallot)
        {
            foreach (var county in counties)
            {
                foreach (var localityTurnouts in county.GroupBy(c => c.Siruta))
                {
                    UpdateLocalityTurnout(ballot, localityTurnouts, turnoutsForBallot);
                }

                UpdateCountyTurnout(ballot, county, dbContext, turnoutsForBallot);
            }
        }

        private void UpdateCountyTurnout(Ballot ballot, IGrouping<string, CsvTurnout> county,
            ApplicationDbContext dbContext, List<Turnout> turnoutsForBallot)
        {
            if (county.Key == "SR")
                return;
            var dbCounty = _dbCounties.FirstOrDefault(c => c.ShortName == county.Key);
            var countyTurnout = turnoutsForBallot.FirstOrDefault(t => t.Division == ElectionDivision.County && t.CountyId == dbCounty.CountyId);
            if (countyTurnout == null)
            {
                countyTurnout = new Turnout
                {
                    Division = ElectionDivision.County,
                    CountyId = dbCounty.CountyId,
                    BallotId = ballot.BallotId
                };
            }

            countyTurnout.EligibleVoters = county.Sum(c => c.EnrolledVoters + c.ComplementaryList); ;
            countyTurnout.TotalVotes = county.Sum(c => c.TotalVotes);
            dbContext.Update(countyTurnout);
        }

        private void UpdateDiaspora(Ballot ballot, IGrouping<string, CsvTurnout> diasporaTurnouts,
            ApplicationDbContext dbContext,
            List<Turnout> existingTurnouts)
        {
            var totalDiasporaVotes = 0;
            foreach (var csvTurnout in diasporaTurnouts.GroupBy(d => d.UAT))
            {
                var csvTurnouts = csvTurnout.ToList();

                var turnout = csvTurnouts.FirstOrDefault();
                var dbCountry = FindCountry(turnout);
                if (dbCountry == null)
                {
                    Console.WriteLine($"{turnout.UAT} not found in the database");
                    continue;
                }
                var countryTurnout = existingTurnouts
                    .FirstOrDefault(t => t.Division == ElectionDivision.Diaspora_Country && t.CountryId == dbCountry.Id);
                if (countryTurnout == null)
                {
                    countryTurnout = new Turnout
                    {
                        Division = ElectionDivision.Diaspora_Country,
                        CountryId = dbCountry.Id,
                        BallotId = ballot.BallotId
                    };
                }
                countryTurnout.EligibleVoters = csvTurnouts.Sum(c => c.EnrolledVoters + c.ComplementaryList);
                countryTurnout.TotalVotes = csvTurnouts.Sum(c => c.TotalVotes);
                totalDiasporaVotes += countryTurnout.TotalVotes;
                dbContext.Update(countryTurnout);
            }

            var diasporaTurnout = new Turnout
            {
                Division = ElectionDivision.Diaspora,
                BallotId = ballot.BallotId,
                TotalVotes = totalDiasporaVotes
            };
            dbContext.Turnouts.Add(diasporaTurnout);
        }

        private Country FindCountry(CsvTurnout turnout)
        {
            return _dbCountries.FirstOrDefault(c => c.Name.EqualsIgnoringAccent(turnout.UAT));
        }

        private void UpdateNationalTurnout(Ballot ballot, ApplicationDbContext dbContext,
            List<Turnout> turnoutsForBallot, List<CsvTurnout> csvTurnouts)
        {
            var nationalTurnout = turnoutsForBallot.FirstOrDefault(t => t.Division == ElectionDivision.National);
            if (nationalTurnout == null)
            {
                nationalTurnout = new Turnout
                {
                    Division = ElectionDivision.National,
                    BallotId = ballot.BallotId
                };
            }

            nationalTurnout.EligibleVoters = csvTurnouts.Sum(t => t.EnrolledVoters + t.ComplementaryList);
            nationalTurnout.TotalVotes = csvTurnouts.Sum(t => t.TotalVotes);
            dbContext.Update(nationalTurnout);
        }
        private void UpdateLocalityTurnout(Ballot ballot, IGrouping<int, CsvTurnout> csvLocality, List<Turnout> turnoutsForBallot)
        {
            var dbLocality = _localities.FirstOrDefault(l => l.Siruta == csvLocality.Key);
            if (dbLocality == null)
                return;
            var dbCounty = _dbCounties.FirstOrDefault(c => c.CountyId == dbLocality.CountyId);
            var turnout = turnoutsForBallot.FirstOrDefault(t => t.BallotId == ballot.BallotId
                                                                && t.Division == ElectionDivision.Locality
                                                                && t.CountyId == dbLocality.CountyId
                                                                && t.LocalityId == dbLocality.LocalityId);

            if (turnout == null)
            {
                turnout = CreateTurnout(dbCounty, dbLocality, ballot);
            }

            turnout.EligibleVoters = csvLocality.Sum(c => c.EnrolledVoters + c.ComplementaryList);
            turnout.TotalVotes = csvLocality.Sum(c => c.TotalVotes);
            turnout.PermanentListsVotes = csvLocality.Sum(c => c.LP);
            turnout.SpecialListsVotes = csvLocality.Sum(c => c.SpecialLists);
            turnout.CorrespondenceVotes = csvLocality.Sum(c => c.MobileBallot);
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

        private static async Task<string> ReadCsvContent(Stream csvStream)
        {
            var buffer = new byte[csvStream.Length];
            await csvStream.ReadAsync(buffer, 0, (int)csvStream.Length);
            var csvContent = Encoding.UTF8.GetString(buffer);
            return csvContent;
        }

        private async Task<Result<VoteMonitoringStats>> GetVoteMonitoringStats()
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
}
