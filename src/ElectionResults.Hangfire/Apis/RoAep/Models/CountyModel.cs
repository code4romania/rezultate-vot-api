using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.Apis.RoAep.Models;

public class CountyModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nce")]
    public string Nce { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }
}
