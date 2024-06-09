using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.Apis.RoAep.SicpvModels;

public class UatModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("county_id")]
    public long CountyId { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("siruta")]
    public long Siruta { get; set; }
}
