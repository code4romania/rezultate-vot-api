using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Extensions;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Z.EntityFramework.Extensions;

namespace ElectionResults.Hangfire.Apis
{
    public class TurnoutCrawler : ITurnoutCrawler
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IHttpClientFactory _httpClientFactory;

        public TurnoutCrawler(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
        {
            _serviceProvider = serviceProvider;
            _httpClientFactory = httpClientFactory;
        }

        public async Task InsertEuroTurnouts()
        {
            const int ballotId = 118;
            await using var dbContext = _serviceProvider.CreateScope().ServiceProvider.GetService<ApplicationDbContext>();
            EntityFrameworkManager.ContextFactory = context => dbContext;
            var counties = await dbContext.Counties.Include(c => c.Localities).ToListAsync();
            var httpClient = _httpClientFactory.CreateClient();
            var dbTurnouts = await dbContext.Turnouts.Where(t => t.BallotId == ballotId).ToListAsync();
            foreach (var dbCounty in counties)
            {
                string url = $"https://prezenta.roaep.ro/europarlamentare09062024/data/json/simpv/presence/presence_{dbCounty.ShortName.ToLower()}_now.json"; // Replace with the actual URL pattern
                string json = await httpClient.GetStringAsync(url);
                var response = JsonConvert.DeserializeObject<CountyResponse>(json);
                var countyData = response!.County.FirstOrDefault(c => c.CountyInfo.Name.GenerateSlug() == dbCounty.Name.GenerateSlug());
                if (countyData == null)
                {
                    countyData = response.County.FirstOrDefault(c => c.CountyInfo.Code == dbCounty.ShortName);
                }

                var countyTurnout = dbTurnouts.FirstOrDefault(t => t.BallotId == ballotId && t.CountyId == dbCounty.CountyId && t.Division == ElectionDivision.County);
                if (countyTurnout == null)
                {
                    countyTurnout = new Turnout
                    {
                        CountyId = dbCounty!.CountyId,
                        BallotId = ballotId,
                        Division = ElectionDivision.County
                    };
                    dbContext.Turnouts.Add(countyTurnout);
                }

                //countyTurnout.EligibleVoters = countyData.InitialCountLp + countyData.InitialCountLc;
                //countyTurnout.TotalVotes = countyData.TotalVotes;
                countyTurnout.CorrespondenceVotes = countyData.CorrespondenceVotes;

                var localities = response.Precinct.GroupBy(p => p.UatInfo.Siruta);

                foreach (var locality in localities)
                {
                    var dbLocality = dbCounty.Localities.FirstOrDefault(l => l.Siruta == locality.Key);
                    if (dbLocality == null)
                    {
                        dbLocality = new Locality
                        {
                            CountyId = dbCounty.CountyId,
                            Siruta = locality.Key,
                            Name = locality.First().UatInfo.Name
                        };
                        dbContext.Localities.Add(dbLocality);
                    }

                    var turnout = dbTurnouts.FirstOrDefault(t => t.BallotId == ballotId && t.LocalityId == dbLocality.LocalityId && t.Division == ElectionDivision.Locality);
                    if (turnout == null)
                    {
                        turnout = new Turnout
                        {
                            LocalityId = dbLocality.LocalityId,
                            BallotId = ballotId,
                            CountyId = dbCounty.CountyId,
                            Division = ElectionDivision.Locality
                        };
                        dbContext.Turnouts.Add(turnout);
                    }

                    //turnout.EligibleVoters = locality.Sum(p => p.InitialCountLp + p.InitialCountLc);
                    //turnout.TotalVotes = locality.Sum(p => p.TotalVotes);
                    turnout.CorrespondenceVotes = locality.Sum(v => v.CorrespondenceVotes);
                    turnout.SpecialListsVotes = locality.Sum(v => v.InitialCountLc);
                }
            }

            var diasporaTurnouts = await GetDiasporaTurnouts(ballotId, dbContext);

            var countiesTurnouts = dbTurnouts.Where(t => t.BallotId == ballotId && t.Division == ElectionDivision.County).ToList();
            var nationalTurnout = dbTurnouts.FirstOrDefault(t => t.BallotId == ballotId && t.Division == ElectionDivision.National);
            if (nationalTurnout == null)
            {
                nationalTurnout = new Turnout
                {
                    BallotId = ballotId,
                    Division = ElectionDivision.National
                };
                dbContext.Turnouts.Add(nationalTurnout);
            }

            //nationalTurnout.EligibleVoters = countiesTurnouts.Sum(t => t.EligibleVoters);
            //nationalTurnout.TotalVotes = countiesTurnouts.Sum(t => t.TotalVotes);
            nationalTurnout.CorrespondenceVotes = countiesTurnouts.Sum(t => t.CorrespondenceVotes);

            foreach (var turnout in diasporaTurnouts)
            {
                var existingTurnout = dbTurnouts.FirstOrDefault(t => t.BallotId == turnout.BallotId && t.CountryId == turnout.CountryId && t.Division == turnout.Division);
                if (existingTurnout != null)
                {
                    //existingTurnout.EligibleVoters = turnout.EligibleVoters;
                    //existingTurnout.TotalVotes = turnout.TotalVotes;
                    existingTurnout.CorrespondenceVotes = turnout.CorrespondenceVotes;
                    existingTurnout.SpecialListsVotes = turnout.SpecialListsVotes;
                }
                else
                {
                    dbContext.Turnouts.Add(turnout);
                }
            }

            await dbContext.SaveChangesAsync();
            Console.WriteLine("done");
        }

        private async Task<List<Turnout>> GetDiasporaTurnouts(int ballotId, ApplicationDbContext dbContext)
        {
            var dbCountries = await dbContext.Countries.ToListAsync();

            string url = $"https://prezenta.roaep.ro/europarlamentare09062024/data/json/simpv/presence/presence_sr_now.json";
            var httpClient = _httpClientFactory.CreateClient();
            string json = await httpClient.GetStringAsync(url);
            var response = JsonConvert.DeserializeObject<CountyResponse>(json);
            var diasporaData = response!.County.FirstOrDefault(c => c.CountyInfo.Code == "SR");
            var turnouts = new List<Turnout>();
            if (diasporaData == null)
            {
                return new List<Turnout>();
            }

            var diasporaTurnout = new Turnout
            {
                CountyId = diasporaData.CountyId,
                //EligibleVoters = diasporaData.InitialCountLp + diasporaData.InitialCountLc,
                //TotalVotes = diasporaData.TotalVotes,
                CorrespondenceVotes = diasporaData.CorrespondenceVotes,
                BallotId = ballotId,
                Division = ElectionDivision.Diaspora
            };
            turnouts.Add(diasporaTurnout);

            var countries = response.Precinct.GroupBy(p => p.UatInfo.Name);

            foreach (var country in countries)
            {
                var dbCountry = dbCountries.FirstOrDefault(c => c.Name.GenerateSlug() == country.FirstOrDefault()!.UatInfo.Name.GenerateSlug());
                if (dbCountry == null)
                {
                    dbCountry = new Country()
                    {
                        Name = country.First().UatInfo.Name
                    };
                    dbContext.Countries.Add(dbCountry);
                }

                var turnout = new Turnout
                {
                    CountryId = dbCountry.Id,
                    BallotId = ballotId,
                    //EligibleVoters = country.Sum(p => p.InitialCountLp + p.InitialCountLc),
                    //TotalVotes = country.Sum(p => p.TotalVotes),
                    Division = ElectionDivision.Diaspora_Country,
                    CorrespondenceVotes = country.Sum(v => v.CorrespondenceVotes),
                    SpecialListsVotes = country.Sum(v => v.InitialCountLc)
                };
                turnouts.Add(turnout);
            }

            return turnouts;
        }

        public class CountyResponse
        {
            [JsonProperty("county")]
            public List<CountyData> County { get; set; }

            [JsonProperty("precinct")]
            public List<PrecinctData> Precinct { get; set; }
        }

        public class CountyData : Votes
        {
            [JsonProperty("county_id")]
            public int CountyId { get; set; }

            [JsonProperty("county")]
            public CountyInfo CountyInfo { get; set; }
        }

        public class Votes
        {
            [JsonProperty("LT")]
            public int TotalVotes { get; set; }
            [JsonProperty("UM")]
            public int CorrespondenceVotes { get; set; }
            [JsonProperty("initial_count_lp")]
            public int InitialCountLp { get; set; }
            [JsonProperty("initial_count_lc")]
            public int InitialCountLc { get; set; }
            [JsonProperty("LP")]
            public int PermanentListVotes { get; set; }
            [JsonProperty("LC")]
            public int ComplementaryListVotes { get; set; }
            [JsonProperty("LS")]
            public int SpecialListVotes { get; set; }
        }

        public class PrecinctData : Votes
        {
            [JsonProperty("locality_id")]
            public int LocalityId { get; set; }

            [JsonProperty("precinct")]
            public PrecinctInfo PrecinctInfo { get; set; }
            [JsonProperty("uat")]
            public UatInfo UatInfo { get; set; }
        }

        public class CountyInfo
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }

            [JsonProperty("code")]
            public string Code { get; set; }
        }

        public class PrecinctInfo
        {
            [JsonProperty("id")]
            public string Id { get; set; }

            [JsonProperty("name")]
            public string Name { get; set; }
        }
    }

    public class UatInfo
    {
        public int Siruta { get; set; }
        public string Name { get; set; }
    }
}
