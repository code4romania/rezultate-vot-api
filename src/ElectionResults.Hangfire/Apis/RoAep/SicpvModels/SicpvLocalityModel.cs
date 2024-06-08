using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.Apis.RoAep.SicpvModels;

public class SicpvLocalityModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("medium")]
    public string Medium { get; set; }

    [JsonPropertyName("uat_id")]
    public int UatId { get; set; }
}