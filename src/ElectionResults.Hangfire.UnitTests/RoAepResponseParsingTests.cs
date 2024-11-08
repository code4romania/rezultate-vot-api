﻿using System.Text.Json;
using System.Text.Json.Serialization;
using ElectionResults.Core.Extensions;
using ElectionResults.Hangfire.Apis.RoAep.SicpvModels;
using ElectionResults.Hangfire.Extensions;

namespace ElectionResults.Hangfire.UnitTests;

public class RoAepResponseParsingTests
{
    [Theory]
    [InlineData("pv_b_final.json")]
    [InlineData("pv_ab_final.json")]
    public void Should_Parse_Response_Correctly(string file)
    {
        var json = File.ReadAllText(file);
        var result = JsonSerializer.Deserialize<PVResultsModel>(json, new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        });
        result.Should().NotBeNull();

        result.Stages[StageCode.PART].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].GetTable().Should().BeEmpty();
        result.Stages[StageCode.PROV].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].GetTable().Should().BeEmpty();
        result.Stages[StageCode.FINAL].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].GetTable().Should().NotBeEmpty();
    }
        
    [Theory]
    [InlineData("pv_sr_part.json")]
    public void Should_ParseSR_Response_Correctly(string file)
    {
        var json = File.ReadAllText(file);
        var result = JsonSerializer.Deserialize<PVResultsModel>(json, new JsonSerializerOptions()
        {
            NumberHandling = JsonNumberHandling.AllowReadingFromString,
        });
        result.Should().NotBeNull();

        result.Stages[StageCode.PROV].Enabled.Should().BeTrue();
        result.Stages[StageCode.PROV].Scopes[ScopeCode.PRCNCT].Categories[CategoryCode.EUP].Table.Should().NotBeEmpty();
        result.Stages[StageCode.PART].Enabled.Should().BeFalse();
        result.Stages[StageCode.FINAL].Enabled.Should().BeFalse();
    }

    //[Fact]
    //public void Should_Parse_PROV_Response_Correctly()
    //{
    //    var json = File.ReadAllText("sicpv-prov-ab.json");
    //    var result = JsonSerializer.Deserialize<PVResultsModel>(json, new JsonSerializerOptions()
    //    {
    //        NumberHandling = JsonNumberHandling.AllowReadingFromString,
    //    });
    //    result.Should().NotBeNull();

    //    result.Stages[StageCode.PART].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].Table.Should().BeEmpty();

    //    result.Stages[StageCode.PROV].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].Table.Should().NotBeEmpty();

    //    result.Stages[StageCode.FINAL].Scopes[ScopeCode.UAT].Categories[CategoryCode.P].Table.Should().BeEmpty();
    //}

    [Theory]
    [InlineData("CICEU - MIHĂIEŞTI", "CICEU-MIHĂIEŞTI")]
    [InlineData("CICEU - MIHĂIEŞTI", "CICeU-MIHĂIEŞTI")]
    [InlineData("CICEU - MIHĂIEŞTI", "CICeU - MIHĂIEŞTI")]
    [InlineData("CICEU - MIHĂIEŞTI", "CICeU -  MIHĂIEŞTI")]
    [InlineData("CICEU - MIHĂIEŞTI", "CICeU MIHĂIEŞTI")]
    [InlineData("CICEU-MIHĂIEŞTI", "CICeU MIHĂIEŞTI")]
    [InlineData("CICEU-MIHĂIeŞTI", "CICeU MIHĂIEŞTI")]
    public void ShouldMatchWeirdChars(string str1, string str2)
    {
        str1.GenerateSlug().Should().Be(str2.GenerateSlug());
    }
}