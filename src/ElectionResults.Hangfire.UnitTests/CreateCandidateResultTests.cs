using System.Globalization;
using System.Text.Json;
using CsvHelper;
using Dapper;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Hangfire.Jobs;
using Microsoft.Data.SqlClient;
using MySql.Data.MySqlClient;

namespace ElectionResults.Hangfire.UnitTests;

public record CRComparison(
    int ballotId,
    int? countryId,
    int? countyId,
    int? localityId,
    ElectionDivision division,
    int? oldPartyId,
    string oldPartyName,
    string oldPartyShortName,
    int? newPartyId,
    string newPartyName,
    string newPartyShortName);

public class CreateCandidateResultTests
{
    string connectionString = "Server=localhost;Port=3306;Database=rv;Uid=root;Pwd=root;";

    [Fact]
    public async Task ShouldFindCorrectPartyId_WhenImportingFromCsv()
    {
        using (var connection = new MySqlConnection(connectionString))
        {
            connection.Open();

            int ballotId = 116;
            string candidateResultsSql = $"SELECT * FROM candidateresults where BallotId = {ballotId};";

            // var candidateResults = connection.Query<CandidateResult>(candidateResultsSql).AsList();

            string jsonString1 = await File.ReadAllTextAsync("candidateResults.json");
            string jsonString2 = await File.ReadAllTextAsync("candidateResults2.json");

            var oldCR = JsonSerializer.Deserialize<List<CandidateResult>>(jsonString1);
            var newCr = JsonSerializer.Deserialize<List<CandidateResult>>(jsonString2);

            var builder =
                new List<CRComparison>();

            for (int i = 0; i < oldCR.Count; i++)
            {
                if (newCr[i].BallotId == oldCR[i].BallotId &&
                    newCr[i].CountryId == oldCR[i].CountryId &&
                    newCr[i].CountyId == oldCR[i].CountyId &&
                    newCr[i].LocalityId == oldCR[i].LocalityId &&
                    newCr[i].Division == oldCR[i].Division)
                {
                    if (newCr[i].PartyId != oldCR[i].PartyId)
                    {
                        builder.Add(new(newCr[i].BallotId, 
                            newCr[i].CountryId, 
                            newCr[i].CountyId,
                            newCr[i].LocalityId,
                            newCr[i].Division, 
                            oldCR[i].PartyId, 
                            oldCR[i].PartyName, 
                            oldCR[i].ShortName, 
                            newCr[i].PartyId,
                            newCr[i].PartyName,
                            newCr[i].ShortName));
                    }
                }
                else
                {
                    int a = 0;
                }
            }

            var options = new JsonSerializerOptions
            {
                WriteIndented = true // For pretty printing
            };

            string builderJson = JsonSerializer.Serialize(builder, options);
            File.WriteAllText("cmp.json", builderJson);
            
            using (var writer = new StreamWriter("comparison.csv"))
            using (var csv = new CsvWriter(writer, CultureInfo.InvariantCulture))
            {
                csv.WriteRecords(builder);
            }
        }
    }
}