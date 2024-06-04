using ElectionResults.Hangfire.Apis.RoAep.Models;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.UnitTests;

public class RoAepResponseParsingTests
{
    [Fact]
    public void Should_Parse_Response_Correctly()
    {
        var json = File.ReadAllText("sicpv-final.json");
        var result = JsonSerializer.Deserialize<PVResultsModel>(json, new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        });
        result.Should().NotBeNull();


        result.Stages[StageCode.PART].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].Table.Should().BeEmpty();
        result.Stages[StageCode.PROV].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].Table.Should().BeEmpty();
        result.Stages[StageCode.FINAL].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].Table.Should().NotBeEmpty();
    }
}