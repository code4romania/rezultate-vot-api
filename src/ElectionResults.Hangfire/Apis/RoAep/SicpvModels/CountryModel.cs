using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.Apis.RoAep.SicpvModels;

public class CountryModel
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }
}