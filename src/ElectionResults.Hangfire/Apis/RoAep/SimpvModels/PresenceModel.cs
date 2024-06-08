using System.Text.Json.Serialization;

namespace ElectionResults.Hangfire.Apis.RoAep.SimpvModels;

public class PresenceModel
{
    [JsonPropertyName("county")]
    public CountyElement[] County { get; set; }

    [JsonPropertyName("precinct")]
    public PrecinctElement[] Precinct { get; set; }
}

public class CountyElement
{
    [JsonPropertyName("initial_count_lp")]
    public int InitialCountLp { get; set; }

    [JsonPropertyName("initial_count_lc")]
    public int InitialCountLc { get; set; }

    [JsonPropertyName("precincts_count")]
    public int PrecinctsCount { get; set; }

    [JsonPropertyName("LP")]
    public int Lp { get; set; }

    [JsonPropertyName("LC")]
    public int Lc { get; set; }

    [JsonPropertyName("LS")]
    public int Ls { get; set; }

    [JsonPropertyName("UM")]
    public int Um { get; set; }

    [JsonPropertyName("LT")]
    public int Lt { get; set; }

    [JsonPropertyName("presence")]
    public double Presence { get; set; }

    [JsonPropertyName("age_ranges")]
    public Dictionary<string, int> AgeRanges { get; set; }

    [JsonPropertyName("medium_u")]
    public int MediumU { get; set; }

    [JsonPropertyName("medium_r")]
    public int MediumR { get; set; }

    [JsonPropertyName("county")]
    public CountyCounty County { get; set; }

    [JsonPropertyName("county_id")]
    public int CountyId { get; set; }
}

public class CountyCounty
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("nce")]
    public int Nce { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("updated_at")]
    public string UpdatedAt { get; set; }
}

public class PrecinctElement
{
    [JsonPropertyName("initial_count_lp")]
    public int InitialCountLp { get; set; }

    [JsonPropertyName("initial_count_lc")]
    public int InitialCountLc { get; set; }

    [JsonPropertyName("LP")]
    public int Lp { get; set; }

    [JsonPropertyName("LC")]
    public int Lc { get; set; }

    [JsonPropertyName("LS")]
    public int Ls { get; set; }

    [JsonPropertyName("UM")]
    public int Um { get; set; }

    [JsonPropertyName("LT")]
    public int Lt { get; set; }

    [JsonPropertyName("presence")]
    public double Presence { get; set; }

    [JsonPropertyName("age_ranges")]
    public Dictionary<string, int> AgeRanges { get; set; }

    [JsonPropertyName("precinct_nr")]
    public int PrecinctNr { get; set; }

    [JsonPropertyName("precinct")]
    public PrecinctPrecinct Precinct { get; set; }

    [JsonPropertyName("locality")]
    public LocalityModel Locality { get; set; }

    [JsonPropertyName("uat")]
    public Uat Uat { get; set; }

    [JsonPropertyName("county")]
    public CountyCounty County { get; set; }

    [JsonPropertyName("county_id")]
    public int CountyId { get; set; }

    [JsonPropertyName("precinct_id")]
    public int PrecinctId { get; set; }

    [JsonPropertyName("uat_id")]
    public int UatId { get; set; }

    [JsonPropertyName("locality_id")]
    public int LocalityId { get; set; }
}

public enum Medium
{
    R, 
    U
};

public class LocalityModel
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public int Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("medium")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Medium Medium { get; set; }

    [JsonPropertyName("id_uat")]
    public int IdUat { get; set; }

    [JsonPropertyName("id_county")]
    public int IdCounty { get; set; }
}

public class PrecinctPrecinct
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("nr")]
    public int Nr { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id_county")]
    public int IdCounty { get; set; }

    [JsonPropertyName("id_locality")]
    public int IdLocality { get; set; }

    [JsonPropertyName("id_uat")]
    public int IdUat { get; set; }
}

public class Uat
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("code")]
    public object Code { get; set; }

    [JsonPropertyName("siruta")]
    public int Siruta { get; set; }

    [JsonPropertyName("id_county")]
    public int IdCounty { get; set; }
}
