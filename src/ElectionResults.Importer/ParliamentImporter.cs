using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Rochas.ExcelToJson;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Extensions;

namespace ElectionResults.Importer
{
    public class ParliamentImporter
    {
        private static readonly HttpClient _httpClient;
        private static readonly List<string> _headerColumns;
        private static readonly List<string> _minoritiesHeaderColumns;
        private static Dictionary<string, int> _countiesMap = new Dictionary<string, int>();

        static ParliamentImporter()
        {
            var httpClientHandler = new HttpClientHandler
            {
            };
            httpClientHandler.ServerCertificateCustomValidationCallback = (message, certificate2, arg3, arg4) => true;
            _httpClient = new HttpClient(httpClientHandler);
            _headerColumns = new List<string>();
            _headerColumns.Add("Number");
            _headerColumns.Add("CountyName");
            _headerColumns.Add("PartyName");
            _headerColumns.Add("Position");
            _headerColumns.Add("Member");
            _headerColumns.Add("CandidateLastName");
            _headerColumns.Add("CandidateFirstName");
            _headerColumns.Add("Function");
            _headerColumns.Add("ListPosition");

            _minoritiesHeaderColumns = new List<string>();
            _minoritiesHeaderColumns.Add("Number");
            _minoritiesHeaderColumns.Add("CountyName");
            _minoritiesHeaderColumns.Add("PartyName");
            _minoritiesHeaderColumns.Add("Member");
            _minoritiesHeaderColumns.Add("CandidateLastName");
            _minoritiesHeaderColumns.Add("CandidateFirstName");
            _minoritiesHeaderColumns.Add("Function");
            _minoritiesHeaderColumns.Add("ListPosition");

            _httpClient.DefaultRequestHeaders.Add("User-Agent", "RezultateVot");
            MapCounties();
        }


        public static async Task Import(ApplicationDbContext dbContext)
        {
            try
            {
                var election = await CreateElection(dbContext);
                var senateBallot = await CreateBallot(dbContext, "Senat", election, BallotType.Senate);
                var houseBallot = await CreateBallot(dbContext, "Camera Deputatilor", election, BallotType.House);
                var counties = await dbContext.Counties.ToListAsync();
                var senators = new List<ExcelCandidate>();
                var deputies = new List<ExcelCandidate>();
                foreach (var county in _countiesMap)
                {
                    var dbCounty = counties.FirstOrDefault(c => c.CountyId == county.Value);
                    var senate = await ImportSenate(county.Key);
                    var deputiesList = await ImportDeputies(county.Key);
                    senators.AddRange(senate);
                    deputies.AddRange(deputiesList);
                    Console.WriteLine($"{senate.Count} senators in {county.Key}");
                    Console.WriteLine($"{deputiesList.Count} deputies in {county.Key}");
                    Console.WriteLine();
                    await ImportCandidatesForBallot(senateBallot, senate, dbContext, county.Value);
                    await ImportCandidatesForBallot(houseBallot, deputiesList, dbContext, county.Value);
                }
            }
            catch (ArgumentOutOfRangeException)
            {
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        }

        private static async Task ImportCandidatesForBallot(Ballot senateBallot, List<ExcelCandidate> candidates,
            ApplicationDbContext dbContext, int countyId)
        {
            foreach (var candidate in candidates)
            {
                var candidateResult = new CandidateResult
                {
                    BallotId = senateBallot.BallotId,
                    CountyId = countyId,
                    Division = ElectionDivision.County,
                    BallotPosition = (int)candidate.ListPosition,
                    Name = candidate.CandidateFirstName + " " + candidate.CandidateLastName,
                    PartyName = candidate.PartyName
                };
                if (countyId.IsDiaspora())
                {
                    candidateResult.Division = ElectionDivision.Diaspora;
                    candidateResult.CountyId = null;
                }

                if (countyId == 16821)
                {
                    candidateResult.CountyId = null;
                }
                dbContext.CandidateResults.Add(candidateResult);
            }

            await dbContext.SaveChangesAsync();
        }

        private static async Task<Ballot> CreateBallot(ApplicationDbContext dbContext, string name, Election election,
            BallotType ballotType)
        {
            var ballot = new Ballot
            {
                Name = name,
                Date = election.Date,
                Round = 0,
                ElectionId = election.ElectionId
            };
            ballot.BallotType = ballotType;
            dbContext.Ballots.Add(ballot);
            await dbContext.SaveChangesAsync();
            return ballot;
        }

        private static async Task<Election> CreateElection(ApplicationDbContext dbContext)
        {
            var existingElection = await dbContext.Elections.FirstOrDefaultAsync(e =>
                e.Category == ElectionCategory.Parliament && e.Date.Year == 2020);
            if (existingElection != null)
            {
                throw new ArgumentOutOfRangeException(nameof(existingElection), "The election already exists");
            }
            var election = new Election();
            election.Category = ElectionCategory.Parliament;
            election.Name = "Alegeri Parlamentare";
            election.Date = new DateTime(2020, 12, 6);
            election.Live = true;
            dbContext.Elections.Add(election);
            await dbContext.SaveChangesAsync();
            return election;
        }

        private static async Task<List<ExcelCandidate>> ImportDeputies(string countyName)
        {
            var url = GetUrl("Camera-Deputatilor", countyName);
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                var fileName = $"{countyName}-deputies.xlsx";
                await File.WriteAllBytesAsync(fileName, bytes);
                var columns = _headerColumns;
                if (countyName.EqualsIgnoringAccent("minoritati"))
                    columns = _minoritiesHeaderColumns;
                var json = ExcelToJsonParser.GetJsonStringFromTabular(fileName, headerColumns: columns.ToArray());
                var candidates = JsonConvert.DeserializeObject<List<ExcelCandidate>>(json);
                File.Delete(fileName);
                return candidates;
            }
            catch (Exception e)
            {
                Console.WriteLine(url);
                Console.WriteLine(e);
            }

            return new List<ExcelCandidate>();
        }

        private static string GetUrl(string type, string countyName)
        {
            countyName = countyName.ToUpper()
                .Replace("Ș", "%C5%9E")
                .Replace("Ț", "%C5%A2")
                .Replace(" ", "-")
                .Replace("Ă", "%C4%82"); ;
            if (countyName.EqualsIgnoringAccent("diaspora"))
            {
                countyName = "Diaspora";
            }
            var url = $"https://parlamentare2020.bec.ro/wp-content/uploads/2020/11/{type}_{countyName}-2020-11-10.xlsx";

            if (countyName.EqualsIgnoringAccent("minoritati"))
            {
                url = $"https://parlamentare2020.bec.ro/wp-content/uploads/2020/11/{type}-BEC-2020-11-10.xlsx";
            }
            return url;
        }

        private static async Task<List<ExcelCandidate>> ImportSenate(string countyName)
        {
            var url = GetUrl("Senat", countyName);
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                var fileName = $"{countyName}-senate.xlsx";
                await File.WriteAllBytesAsync(fileName, bytes);

                var json = ExcelToJsonParser.GetJsonStringFromTabular(fileName, headerColumns: _headerColumns.ToArray());
                var candidates = JsonConvert.DeserializeObject<List<ExcelCandidate>>(json);
                File.Delete(fileName);
                return candidates;
            }
            catch (Exception e)
            {
                Console.WriteLine(url);
                Console.WriteLine(e);
                return new List<ExcelCandidate>();
            }
        }
        private static void MapCounties()
        {
            _countiesMap.Add("ALBA", 1);
            _countiesMap.Add("ARAD", 589);
            _countiesMap.Add("ARGEȘ", 1122);
            _countiesMap.Add("BACĂU", 1814);
            _countiesMap.Add("BIHOR", 2513);
            _countiesMap.Add("BISTRIȚA-NĂSĂUD", 3325);
            _countiesMap.Add("BOTOȘANI", 3747);
            _countiesMap.Add("BRĂILA", 4231);
            _countiesMap.Add("BRAȘOV", 4481);
            _countiesMap.Add("BUZĂU", 4862);
            _countiesMap.Add("CĂLĂRAȘI", 5400);
            _countiesMap.Add("CARAȘ-SEVERIN", 5676);
            _countiesMap.Add("CLUJ", 6150);
            _countiesMap.Add("CONSTANȚA", 6793);
            _countiesMap.Add("COVASNA", 7308);
            _countiesMap.Add("DÂMBOVIȚA", 7588);
            _countiesMap.Add("DOLJ", 8114);
            _countiesMap.Add("GALAȚI", 8747);
            _countiesMap.Add("GIURGIU", 9134);
            _countiesMap.Add("GORJ", 9449);
            _countiesMap.Add("HARGHITA", 9866);
            _countiesMap.Add("HUNEDOARA", 10251);
            _countiesMap.Add("IALOMIȚA", 10886);
            _countiesMap.Add("IAȘI", 11169);
            _countiesMap.Add("ILFOV", 11830);
            _countiesMap.Add("MARAMUREȘ", 12053);
            _countiesMap.Add("MEHEDINȚI", 12539);
            _countiesMap.Add("MUNICIPIUL BUCUREȘTI", 12913);
            _countiesMap.Add("MUREȘ", 13227);
            _countiesMap.Add("NEAMȚ", 13933);
            _countiesMap.Add("OLT", 14365);
            _countiesMap.Add("PRAHOVA", 14895);
            _countiesMap.Add("SĂLAJ", 15592);
            _countiesMap.Add("SATU MARE", 16002);
            _countiesMap.Add("SIBIU", 16392);
            _countiesMap.Add("DIASPORA", 16820);
            _countiesMap.Add("MINORITĂȚI", 16821);
            _countiesMap.Add("SUCEAVA", 17391);
            _countiesMap.Add("TELEORMAN", 18142);
            _countiesMap.Add("TIMIȘ", 18648);
            _countiesMap.Add("TULCEA", 19284);
            _countiesMap.Add("VÂLCEA", 19520);
            _countiesMap.Add("VASLUI", 20009);
            _countiesMap.Add("VRANCEA", 20574);
        }
    }
}
