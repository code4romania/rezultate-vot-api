using System.Runtime.Serialization;

namespace ElectionResults.Core.Endpoints.Response
{
    public enum BallotType
    {
        Referendum,
        President,
        Senate,
        House,
        [EnumMember(Value = "local_council")]
        LocalCouncil,
        [EnumMember(Value = "county_council")]
        CountyCouncil,
        Mayor,
        [EnumMember(Value = "european_parliament")]
        EuropeanParliament,
        [EnumMember(Value = "county_council_president")]
        CountyCouncilPresident,
        [EnumMember(Value = "capital_city_mayor")]
        CapitalCityMayor,
        [EnumMember(Value = "capital_city_council")]
        CapitalCityCouncil,
    }
}