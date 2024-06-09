using System.Text.Json;
using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.Apis.RoAep.SicpvModels;

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
    /// <summary>
    /// if  (scopeCode = PRCNCT) -> precinct_id
    /// if  (scopeCode = UAT)    -> uat_id
    /// if  (scopeCode = CNTY)   -> county_id 
    /// if  (scopeCode = CNTRY)  -> RO 
    /// </summary>
    [JsonPropertyName("table")]
    [JsonConverter(typeof(CategoryTableConverter))]
    public Dictionary<string, TableEntryModel>? Table { get; set; }

    public Dictionary<string, TableEntryModel> GetTable()
    {
        return Table ??= new Dictionary<string, TableEntryModel>();
    }
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

    [JsonPropertyName("fields")]
    public List<FieldModel> Fields { get; set; }

    [JsonPropertyName("report_id")]
    public string ReportId { get; set; }

    [JsonPropertyName("report_version")]
    public string ReportVersion { get; set; }

    [JsonPropertyName("precinct_id")]
    public string PrecinctId { get; set; }

    [JsonPropertyName("precinct_nr")]
    public string PrecinctNr { get; set; }

    [JsonPropertyName("precinct_name")]
    public string PrecinctName { get; set; }

    [JsonPropertyName("uat_id")]
    public string UatId { get; set; }

    [JsonPropertyName("uat_name")]
    public string UatName { get; set; }

    [JsonPropertyName("uat_siruta")]
    public string UatSiruta { get; set; }

    [JsonPropertyName("county_id")]
    public long? CountyId { get; set; }

    [JsonPropertyName("county_code")]
    public string CountyCode { get; set; }

    [JsonPropertyName("county_nce")]
    public string CountyNce { get; set; }

    [JsonPropertyName("county_name")]
    public string CountyName { get; set; }

    [JsonPropertyName("precinct_county_id")]
    public string PrecinctCountyId { get; set; }

    [JsonPropertyName("precinct_county_code")]
    public string PrecinctCountyCode { get; set; }

    [JsonPropertyName("precinct_county_name")]
    public string PrecinctCountyName { get; set; }

    [JsonPropertyName("precinct_county_nce")]
    public int? PrecinctCountyNce { get; set; }

    [JsonPropertyName("report_type_scope_code")]
    public string ReportTypeScopeCode { get; set; }

    [JsonPropertyName("report_type_code")]
    public string ReportTypeCode { get; set; }

    [JsonPropertyName("report_type_category_code")]
    public string ReportTypeCategoryCode { get; set; }

    [JsonPropertyName("report_stage_code")]
    public string ReportStageCode { get; set; }
}


public class FieldNames
{

    /// <summary>
    /// Numărul total al alegătorilor prevăzuți în listele electorale din circumscripția electorală<br/>
    /// <b>a = a1 + a2 + a3 + a4</b>
    /// </summary>
    public const string a = "a";

    /// <summary>
    /// Numărul total al alegătorilor potrivit listei electorale permanente<br/>
    /// <b> a1 >= b1</b>
    /// </summary>
    public const string a1 = "a1";


    /// <summary>
    /// Numărul total al alegătorilor potrivit copiilor de pe listele electorale complementare<br/>
    /// <b>a2 >= b2</b>
    /// </summary>
    public const string a2 = "a2";


    /// <summary>
    /// Numărul total al alegătorilor potrivit listelor electorale suplimentare<br/>
    /// <b> a3 >= b3</b>
    /// </summary>
    public const string a3 = "a3";


    /// <summary>
    /// Numărul total al alegătorilor în cazul cărora s-a folosit urna specială<br/>
    ///<b>a4 >= b4</b>
    /// </summary>
    public const string a4 = "a4";


    /// <summary>
    /// Numărul total al alegătorilor <b>care s-au prezentat la urne</b>, înscriși în listele electorale din circumscripția electorală<br/>
    ///<b>b = b1 + b2 + b3 + b4</b>
    /// </summary>
    public const string b = "b";


    /// <summary>
    /// Numărul total al alegătorilor <b>care s-au prezentat la urne</b>, înscriși în listele electorale permanente
    /// </summary>
    public const string b1 = "b1";


    /// <summary>
    /// Numărul total al alegătorilor <b>care s-au prezentat la urne</b>, înscriși în copiile de pe listele electorale complementare
    /// </summary>
    public const string b2 = "b2";


    /// <summary>
    /// Numărul total al alegătorilor <b>care s-au prezentat la urne</b>, înscriși în listele electorale suplimentare
    /// </summary>
    public const string b3 = "b3";


    /// <summary>
    /// Numărul total al alegătorilor <b>care s-au prezentat la urne</b>, în cazul cărora s-a folosit urna specială
    /// </summary>
    public const string b4 = "b4";


    /// <summary>
    ///  Numărul total al voturilor valabil exprimate<br/>
    /// c <= b - d<br/>
    /// <b>c = Suma voturilor valabil exprimate la g</b>
    /// </summary>
    public const string c = "c";


    /// <summary>
    ///  Numărul total al voturilor nule 
    /// </summary>
    public const string d = "d";
}
public class FieldModel
{
    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("value")]
    public int? Value { get; set; }
}

public class VoteModel
{
    [JsonPropertyName("candidate")]
    public string Candidate { get; set; }

    [JsonPropertyName("party")]
    public string Party { get; set; }

    [JsonPropertyName("votes")]
    public int? Votes { get; set; }

    [JsonPropertyName("mandates1")]
    public int? Mandates1 { get; set; }

    [JsonPropertyName("mandates2")]
    public int? Mandates2 { get; set; }
}
