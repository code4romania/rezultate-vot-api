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
    public long InitialCountLp { get; set; }

    [JsonPropertyName("initial_count_lc")]
    public long InitialCountLc { get; set; }

    [JsonPropertyName("precincts_count")]
    public long PrecinctsCount { get; set; }

    [JsonPropertyName("LP")]
    public long Lp { get; set; }

    [JsonPropertyName("LC")]
    public long Lc { get; set; }

    [JsonPropertyName("LS")]
    public long Ls { get; set; }

    [JsonPropertyName("UM")]
    public long Um { get; set; }

    [JsonPropertyName("LT")]
    public long Lt { get; set; }

    [JsonPropertyName("presence")]
    public double Presence { get; set; }

    [JsonPropertyName("age_ranges")]
    public Dictionary<string, long> AgeRanges { get; set; }

    [JsonPropertyName("medium_u")]
    public long MediumU { get; set; }

    [JsonPropertyName("medium_r")]
    public long MediumR { get; set; }

    [JsonPropertyName("county")]
    public CountyCounty County { get; set; }

    [JsonPropertyName("county_id")]
    public long CountyId { get; set; }
}

public class CountyCounty
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("nce")]
    public long Nce { get; set; }

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
    public long InitialCountLp { get; set; }

    [JsonPropertyName("initial_count_lc")]
    public long InitialCountLc { get; set; }

    [JsonPropertyName("LP")]
    public long Lp { get; set; }

    [JsonPropertyName("LC")]
    public long Lc { get; set; }

    [JsonPropertyName("LS")]
    public long Ls { get; set; }

    [JsonPropertyName("UM")]
    public long Um { get; set; }

    [JsonPropertyName("LT")]
    public long Lt { get; set; }

    [JsonPropertyName("presence")]
    public double Presence { get; set; }

    [JsonPropertyName("age_ranges")]
    public Dictionary<string, long> AgeRanges { get; set; }

    [JsonPropertyName("precinct_nr")]
    public long PrecinctNr { get; set; }

    [JsonPropertyName("precinct")]
    public PrecinctPrecinct Precinct { get; set; }

    [JsonPropertyName("locality")]
    public Locality Locality { get; set; }

    [JsonPropertyName("uat")]
    public Uat Uat { get; set; }

    [JsonPropertyName("county")]
    public CountyCounty County { get; set; }

    [JsonPropertyName("county_id")]
    public long CountyId { get; set; }

    [JsonPropertyName("precinct_id")]
    public long PrecinctId { get; set; }

    [JsonPropertyName("uat_id")]
    public long UatId { get; set; }

    [JsonPropertyName("locality_id")]
    public long LocalityId { get; set; }
}

public enum Medium
{
    R, 
    U
};

public class Locality
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("code")]
    public long Code { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("medium")]
    [JsonConverter(typeof(JsonStringEnumConverter))]
    public Medium Medium { get; set; }

    [JsonPropertyName("id_uat")]
    public long IdUat { get; set; }

    [JsonPropertyName("id_county")]
    public long IdCounty { get; set; }
}

public class PrecinctPrecinct
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("code")]
    public string Code { get; set; }

    [JsonPropertyName("nr")]
    public long Nr { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("id_county")]
    public long IdCounty { get; set; }

    [JsonPropertyName("id_locality")]
    public long IdLocality { get; set; }

    [JsonPropertyName("id_uat")]
    public long IdUat { get; set; }
}

public class Uat
{
    [JsonPropertyName("id")]
    public long Id { get; set; }

    [JsonPropertyName("name")]
    public string Name { get; set; }

    [JsonPropertyName("code")]
    public object Code { get; set; }

    [JsonPropertyName("siruta")]
    public long Siruta { get; set; }

    [JsonPropertyName("id_county")]
    public long IdCounty { get; set; }
}
