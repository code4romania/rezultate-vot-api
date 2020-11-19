using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Rochas.ExcelToJson;
using System;
using System.Collections.Generic;
using System.IO;
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
        }

        public static async Task Import(ApplicationDbContext dbContext)
        {
            var election = await CreateElection(dbContext);
            var senateBallot = await CreateBallot(dbContext, "Senat", election, BallotType.Senate);
            var houseBallot = await CreateBallot(dbContext, "Camera Deputatilor", election, BallotType.House);
            var counties = await dbContext.Counties.ToListAsync();
            var senators = new List<ExcelCandidate>();
            var deputies = new List<ExcelCandidate>();
            foreach (var county in counties)
            {
                var senate = await ImportSenate(county);
                var deputiesList = await ImportDeputies(county);
                senators.AddRange(senate);
                deputies.AddRange(deputiesList);
                Console.WriteLine($"{senate.Count} senators in {county.Name}");
                Console.WriteLine($"{deputiesList.Count} deputies in {county.Name}");
                Console.WriteLine();
                await ImportCandidatesForBallot(senateBallot, senate, dbContext, county);
                await ImportCandidatesForBallot(houseBallot, deputiesList, dbContext, county);
            }

            Console.WriteLine(senators.Count);
        }

        private static async Task ImportCandidatesForBallot(Ballot senateBallot, List<ExcelCandidate> candidates,
            ApplicationDbContext dbContext, County county)
        {
            foreach (var candidate in candidates)
            {
                var candidateResult = new CandidateResult
                {
                    BallotId = senateBallot.BallotId,
                    CountyId = county.CountyId,
                    Division = ElectionDivision.County,
                    BallotPosition = (int) candidate.ListPosition,
                    Name = candidate.CandidateFirstName + " " + candidate.CandidateLastName,
                    PartyName = candidate.PartyName
                };
                if (county.CountyId.IsDiaspora())
                {
                    candidateResult.Division = ElectionDivision.Diaspora;
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
            var election = new Election();
            election.Category = ElectionCategory.Parliament;
            election.Name = "Alegeri Parlamentare";
            election.Date = new DateTime(2020, 12, 6);
            election.Live = true;
            dbContext.Elections.Add(election);
            await dbContext.SaveChangesAsync();
            return election;
        }

        private static async Task<List<ExcelCandidate>> ImportDeputies(County county)
        {
            var url = GetUrl("Camera-Deputatilor", county);
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                var fileName = $"{county.Name}-deputies.xlsx";
                await File.WriteAllBytesAsync(fileName, bytes);
                var columns = _headerColumns;
                if (county.Name.EqualsIgnoringAccent("minoritati"))
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

        private static string GetUrl(string type, County county)
        {
            var countyName = county.Name.ToUpper()
                .Replace("Ș", "%C5%9E")
                .Replace("Ț", "%C5%A2")
                .Replace(" ", "-")
                .Replace("Ă", "%C4%82"); ;
            if (countyName.EqualsIgnoringAccent("diaspora"))
            {
                countyName = "Diaspora";
            }
            var url = $"https://parlamentare2020.bec.ro/wp-content/uploads/2020/11/{type}_{countyName}-2020-11-10.xlsx";

            if (county.Name.EqualsIgnoringAccent("minoritati"))
            {
                url = $"https://parlamentare2020.bec.ro/wp-content/uploads/2020/11/{type}-BEC-2020-11-10.xlsx";
            }
            return url;
        }

        private static async Task<List<ExcelCandidate>> ImportSenate(County county)
        {
            var url = GetUrl("Senat", county);
            try
            {
                var bytes = await _httpClient.GetByteArrayAsync(url);
                var fileName = $"{county.Name}-senate.xlsx";
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
    }

    public class ExcelCandidate
    {
        public float Number { get; set; }
        public string CountyName { get; set; }
        public string PartyName { get; set; }
        public float Position { get; set; }
        public string Member { get; set; }
        public string CandidateLastName { get; set; }
        public string CandidateFirstName { get; set; }
        public string Function { get; set; }
        public float ListPosition { get; set; }
    }

}
