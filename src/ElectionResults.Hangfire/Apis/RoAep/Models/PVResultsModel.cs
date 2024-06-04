using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.Apis.RoAep.Models;

public class PVResultsModel
{
    [JsonPropertyName("stages")]
    public Dictionary<StageCode, StageModel> Stages { get; set; }
}

public class StageModel
{
    [JsonPropertyName("enabled")]
    public bool Enabled { get; set; }

    [JsonPropertyName("scopes")]
    public Dictionary<ScopeCode, ScopeModel> Scopes { get; set; }
}

public class ScopeModel
{
    [JsonPropertyName("categories")]
    public Dictionary<CategoryCode, CategoryModel> Categories { get; set; }
}

public class CategoryModel
{
    [JsonPropertyName("table")]
    [JsonConverter(typeof(CategoryTableConverter))]
    public Dictionary<string, TableEntryModel> Table { get; set; }
}


public class CategoryTableConverter : JsonConverter<Dictionary<string, TableEntryModel>>
{

    public override Dictionary<string, TableEntryModel> Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        if (reader.TokenType == JsonTokenType.StartArray)
        {
            reader.Read(); // Move past the StartArray token
            if (reader.TokenType == JsonTokenType.EndArray)
            {
                return new Dictionary<string, TableEntryModel>();
            }

            throw new JsonException("Expected end of array.");
        }

        if (reader.TokenType == JsonTokenType.StartObject)
        {
            return JsonSerializer.Deserialize<Dictionary<string, TableEntryModel>>(ref reader, options);
        }

        throw new JsonException("Expected start of array or object.");
    }

    public override void Write(Utf8JsonWriter writer, Dictionary<string, TableEntryModel> value, JsonSerializerOptions options)
    {
        if (value == null || value.Count == 0)
        {
            writer.WriteStartArray();
            writer.WriteEndArray();
        }
        else
        {
            JsonSerializer.Serialize(writer, value, options);
        }
    }
}

public class TableEntryModel
{
    [JsonPropertyName("votes")]
    public List<VoteModel> Votes { get; set; }
}

public class VoteModel
{
    [JsonPropertyName("candidate")]
    public string Candidate { get; set; }

    [JsonPropertyName("party")]
    public object Party { get; set; }

    [JsonPropertyName("votes")]
    public long Votes { get; set; }

    [JsonPropertyName("mandates")]
    public long Mandates { get; set; }
}
