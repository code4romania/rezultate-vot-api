using ElectionResults.Hangfire.Apis.RoAep.Models;
using Refit;

namespace ElectionResults.Hangfire.Apis.RoAep;

public interface IRoAepApi
{
    [Get("/{electionRound}/data/json/simpv/lists/counties.json")]
    Task<CountyModel[]> ListCounties([AliasAs("electionRound")] string electionRound);

    [Get("/{electionRound}/data/json/simpv/lists/uats_{countyCode}.json")]
    Task<UatModel[]> ListUats([AliasAs("electionRound")] string electionRound, [AliasAs("countyCode")] string countyCode);

    [Get("/{electionRound}/data/json/simpv/lists/localities_{countyCode}.json")]
    Task<LocalityModel[]> ListLocalities([AliasAs("electionRound")] string electionRound, [AliasAs("countyCode")] string countyCode);

    [Get("/{electionRound}/data/json/sicpv/pv/pv_{countyCode}_{stageCode}.json")]
    Task<PVResultsModel> GetPVForCounty([AliasAs("electionRound")] string electionRound, [AliasAs("countyCode")] string countyCode, [AliasAs("stageCode")] StageCode stage);


}