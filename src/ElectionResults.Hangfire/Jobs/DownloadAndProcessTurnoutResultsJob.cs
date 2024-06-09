using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.RoAep;
using ElectionResults.Hangfire.Apis.RoAep.SicpvModels;
using Microsoft.EntityFrameworkCore;
using ElectionResults.Hangfire.Extensions;
using Z.EntityFramework.Plus;

namespace ElectionResults.Hangfire.Jobs;

public class DownloadAndProcessTurnoutResultsJob(IRoAepApi roAepApi,
    ApplicationDbContext context,
    ILogger<DownloadAndProcessTurnoutResultsJob> logger)
{
    private const string DiasporaCountyCode = "SR";

    public async Task Run(string electionRoundKey, int electionRoundId, bool hasDiaspora)
    {
        var electionRound = context.Elections.FirstOrDefault(x => x.ElectionId == electionRoundId);
        if (electionRound == null)
        {
            throw new ArgumentException($"Election round {electionRoundId} does not exist!");
        }

        var counties = (await context.Counties.FromCacheAsync(CacheKeys.RoCounties)).ToList();
        var countries = context.Countries.FromCache(CacheKeys.Countries).ToList();
        var localities = context.Localities.FromCache(CacheKeys.RoLocalities).ToList();

        var ballots = await context
            .Ballots
            .Where(b => b.ElectionId == electionRound.ElectionId)
            .ToListAsync();

        // Stages are registered by their priority, subsequent stages should override the data!
        // From ROAEP advice: daca nu intereseaza separarea datelor din PV in cele 3 categorii (provizorii, partiale si finale), se pot acesa doar endpoint-urile de partiale, pentru ca acela contin toate datele in ultima versiunea. 
        StageCode[] stages =
        [
            //StageCode.PROV,
            StageCode.PART,
            //StageCode.FINAL
        ];

        foreach (var stageCode in stages)
        {
            await ProcessStage(electionRound, electionRoundKey, stageCode, hasDiaspora, ballots, countries, counties, localities);
        }
    }

    private async Task ProcessStage(Election electionRound,
        string electionRoundKey,
        StageCode stageCode,
        bool hasDiaspora,
        List<Ballot> ballots,
        List<Country> countries,
        List<County> counties,
        List<Locality> localities)
    {
        var countiesResults = new Dictionary<string, Dictionary<ScopeCode, ScopeModel>>();

        foreach (var county in counties)
        {
            var countyResult = await roAepApi.GetPVForCounty(electionRoundKey, county.ShortName, stageCode);
            var (hasValue, stage) = GetStageScopeData(electionRoundKey, stageCode, countyResult, county.ShortName);

            if (hasValue)
            {
                countiesResults.Add(county.ShortName, stage);
            }
        }

        Dictionary<CategoryCode, CategoryModel> diasporaResult = default!;
        if (hasDiaspora)
        {
            var diasporaData = await roAepApi.GetPVForCounty(electionRoundKey, DiasporaCountyCode, stageCode);
            var (hasValue, stage) = GetStageScopeCategoriesData(electionRoundKey, stageCode, diasporaData, DiasporaCountyCode, ScopeCode.CNTRY);

            if (hasValue)
            {
                diasporaResult = stage;
            }
        }

        foreach (var ballot in ballots)
        {
            var turnoutsForBallot = await context
                .Turnouts
                .Where(t => t.BallotId == ballot.BallotId)
                .ToListAsync();

            if (hasDiaspora && diasporaResult is not null)
            {
                UpdateDiasporaTurnouts(countries, ballot, diasporaResult, turnoutsForBallot);
            }

            foreach (var countyResult in countiesResults)
            {
                var county = counties.First(x => x.ShortName == countyResult.Key);
                var countyLocalities = localities.Where(x => x.CountyId == county.CountyId).ToList();
                UpdateCountyTurnout(countyResult.Value[ScopeCode.CNTY].Categories, ballot, turnoutsForBallot, county);
                UpdateLocalitiesTurnouts(countyResult.Value[ScopeCode.UAT].Categories, ballot, turnoutsForBallot, county, countyLocalities);
            }

            // each county result has data about the country as well
            var firstResult = countiesResults.Values.FirstOrDefault();
            if (firstResult is not null && firstResult.ContainsKey(ScopeCode.CNTRY))
            {
                UpdateNationalTurnout(firstResult[ScopeCode.CNTY].Categories, ballot, turnoutsForBallot);
            }
        }

        context.SaveChanges();
    }

    private (bool hasValue, Dictionary<CategoryCode, CategoryModel> data) GetStageScopeCategoriesData(string electionRoundKey,
        StageCode stageCode,
        PVResultsModel countyResult,
        string countyCode,
        ScopeCode scopeCode)
    {
        if (countyResult.Stages.TryGetValue(stageCode, out var stage))
        {
            if (stage.Enabled)
            {
                if (stage.Scopes.TryGetValue(scopeCode, out var scope))
                {
                    return (true, scope.Categories);
                }

                logger.LogWarning("Scope {scopeCode} not present in {electionRoundKey} {countyCode} {stageCode}", scopeCode, electionRoundKey, DiasporaCountyCode, stageCode);
            }

            logger.LogWarning("Stage not enabled on {electionRoundKey} {countyCode} {stageCode}", electionRoundKey, countyCode, stageCode);
        }
        else
        {
            logger.LogWarning("Stage not found on {electionRoundKey} {countyCode} {stageCode}", electionRoundKey, countyCode, stageCode);
        }

        return (false, default!);
    }

    private (bool hasValue, Dictionary<ScopeCode, ScopeModel> data) GetStageScopeData(string electionRoundKey,
        StageCode stageCode,
        PVResultsModel countyResult,
        string countyCode)
    {
        if (countyResult.Stages.TryGetValue(stageCode, out var stage))
        {
            if (stage.Enabled)
            {
                return (true, stage.Scopes);
            }

            logger.LogWarning("Stage not enabled on {electionRoundKey} {countyCode} {stageCode}", electionRoundKey, countyCode, stageCode);
        }
        else
        {
            logger.LogWarning("Stage not found on {electionRoundKey} {countyCode} {stageCode}", electionRoundKey, countyCode, stageCode);
        }

        return (false, default!);
    }

    /// <summary>
    /// this method should be checked. e
    /// </summary>
    /// <param name="countries"></param>
    /// <param name="ballot"></param>
    /// <param name="results"></param>
    /// <param name="existingTurnouts"></param>
    private void UpdateDiasporaTurnouts(
        List<Country> countries,
        Ballot ballot,
        Dictionary<CategoryCode, CategoryModel> results,
        List<Turnout> existingTurnouts)
    {
        // Table contains CountryId and results for it
        var category = MapBallotTypeToCategoryCode(ballot.BallotType);
        var hasCategory = results.TryGetValue(category, out var resultsCategory);
        if (hasCategory == false)
        {
            logger.LogWarning("Could not find requested {category} {ballotType}", category, ballot.BallotType);
            return;
        }

        int totalDiasporaEligibleVoters = 0;
        int totalDiasporaTotalVotes = 0;
        int totalDiasporaNumberOfValidVotes = 0;
        int totalDiasporaNumberOfNullVotes = 0;

        foreach (var turnout in resultsCategory!.Table.Values)
        {
            var dbCountry = FindCountry(countries, turnout);

            if (dbCountry == null)
            {
                logger.LogWarning($"{turnout.UatName} not found in the database");
                continue;
            }

            var eligibleVoters = turnout.Fields.TryGetTotalNumberOfEligibleVoters();
            var totalVotes = turnout.Fields.TryGetNumberOfVotes();
            var numberOfValidVotes = turnout.Fields.TryGetNumberOfValidVotes();
            var numberOfNullVotes = turnout.Fields.TryGetNumberOfNullVotes();

            var countryTurnout = existingTurnouts.FirstOrDefault(t => t.Division == ElectionDivision.Diaspora_Country && t.CountryId == dbCountry.Id);

            if (countryTurnout == null)
            {
                countryTurnout = Turnout.CreateForDiasporaCountry(ballot, dbCountry, eligibleVoters, totalVotes, numberOfValidVotes, numberOfNullVotes);
                context.Turnouts.AddAsync(countryTurnout);
            }
            else
            {
                countryTurnout.Update(eligibleVoters, totalVotes, numberOfValidVotes, numberOfNullVotes);
            }

            totalDiasporaEligibleVoters = turnout.Fields.TryGetTotalNumberOfEligibleVoters();
            totalDiasporaTotalVotes = turnout.Fields.TryGetNumberOfVotes();
            totalDiasporaNumberOfValidVotes = turnout.Fields.TryGetNumberOfValidVotes();
            totalDiasporaNumberOfNullVotes = turnout.Fields.TryGetNumberOfNullVotes();
        }
        var diasporaTurnout = existingTurnouts.FirstOrDefault(t => t.Division == ElectionDivision.Diaspora);

        if (diasporaTurnout == null)
        {
            diasporaTurnout = Turnout.CreateForDiaspora(ballot, totalDiasporaEligibleVoters, totalDiasporaTotalVotes, totalDiasporaNumberOfValidVotes, totalDiasporaNumberOfNullVotes);
            context.Turnouts.AddAsync(diasporaTurnout);
        }
        else
        {
            diasporaTurnout.Update(totalDiasporaEligibleVoters, totalDiasporaTotalVotes, totalDiasporaNumberOfValidVotes, totalDiasporaNumberOfNullVotes);
        }
    }

    private void UpdateLocalitiesTurnouts(Dictionary<CategoryCode, CategoryModel> uatsResults,
        Ballot ballot,
        List<Turnout> turnoutsForBallot,
        County county,
        List<Locality> localities)
    {
        var category = MapBallotTypeToCategoryCode(ballot.BallotType);

        if (!uatsResults.ContainsKey(category))
        {
            logger.LogWarning("Could not find requested {category} {ballotType} in response for {countyCode}", category, ballot.BallotType, county.ShortName);
            return;
        }

        foreach (var uatTurnout in uatsResults[category].Table)
        {
            var locality = localities.First(x => x.Siruta == int.Parse(uatTurnout.Value.UatSiruta));
            var turnout = turnoutsForBallot.FirstOrDefault(t => t.BallotId == ballot.BallotId
                                                                && t.Division == ElectionDivision.Locality
                                                                && t.CountyId == county.CountyId
                                                                && t.LocalityId == locality.LocalityId);


            var totalNumberOfEligibleVoters = uatTurnout.Value.Fields.TryGetTotalNumberOfEligibleVoters();
            var totalNumberOfVotes = uatTurnout.Value.Fields.TryGetNumberOfVotes();
            var numberOfValidVotes = uatTurnout.Value.Fields.TryGetNumberOfValidVotes();
            var numberOfNullVotes = uatTurnout.Value.Fields.TryGetNumberOfNullVotes();

            if (turnout == null)
            {
                turnout = Turnout.CreateForUat(ballot, county, locality, totalNumberOfEligibleVoters, totalNumberOfVotes, numberOfValidVotes, numberOfNullVotes);
                context.Turnouts.AddAsync(turnout);
            }
            else
            {
                turnout.Update(totalNumberOfEligibleVoters, totalNumberOfVotes, numberOfValidVotes, numberOfNullVotes);
            }
        }
    }

    private void UpdateCountyTurnout(Dictionary<CategoryCode, CategoryModel> countyResults,
        Ballot ballot,
        List<Turnout> turnoutsForBallot,
        County county)
    {
        var category = MapBallotTypeToCategoryCode(ballot.BallotType);

        if (!countyResults.ContainsKey(category))
        {
            logger.LogWarning("Could not find requested {category} {ballotType} in response for {countyCode}",
                category, ballot.BallotType, county.ShortName);
            return;
        }

        // When scope is CNTY we have only one entry with key = countyId from AEP
        // It could happen that table is empty.
        if (!countyResults[category].Table.Any())
        {
            logger.LogWarning("No data for {category} {ballotType} in response for {countyCode}",
                category, ballot.BallotType, county.ShortName);
            return;
        }

        var countyResult = countyResults[category].Table.First();

        var turnout = turnoutsForBallot.FirstOrDefault(t => t.Division == ElectionDivision.County && t.CountyId == county.CountyId);

        var totalNumberOfEligibleVoters = countyResult.Value.Fields.TryGetTotalNumberOfEligibleVoters();
        var totalNumberOfVotes = countyResult.Value.Fields.TryGetNumberOfVotes();
        var numberOfValidVotes = countyResult.Value.Fields.TryGetNumberOfValidVotes();
        var numberOfNullVotes = countyResult.Value.Fields.TryGetNumberOfNullVotes();

        if (turnout == null)
        {
            turnout = Turnout.CreateForCounty(ballot, county, totalNumberOfEligibleVoters, totalNumberOfVotes, numberOfValidVotes, numberOfNullVotes);
            context.Turnouts.Add(turnout);
        }
        else
        {
            turnout.Update(totalNumberOfEligibleVoters, totalNumberOfVotes, numberOfValidVotes, numberOfNullVotes);
        }

        //var winners =  countyResult.Value.Votes.
    }

    private void UpdateNationalTurnout(Dictionary<CategoryCode, CategoryModel> countryData,
        Ballot ballot,
        List<Turnout> turnoutsForBallot)
    {
        var category = MapBallotTypeToCategoryCode(ballot.BallotType);

        if (!countryData.ContainsKey(category))
        {
            logger.LogWarning("Could not find requested {category} {ballotType}  for country",
                category, ballot.BallotType);
            return;
        }

        // It could happen that table is empty.
        if (!countryData[category].Table.Any())
        {
            logger.LogWarning("No data for {category} {ballotType} for Romania",
                category, ballot.BallotType);
            return;
        }

        // When scope is CNTRY we have only one entry  with key = RO
        var countryResult = countryData[category].Table.First();

        var turnout = turnoutsForBallot.FirstOrDefault(t => t.Division == ElectionDivision.National);

        var totalNumberOfEligibleVoters = countryResult.Value.Fields.TryGetTotalNumberOfEligibleVoters();
        var totalNumberOfVotes = countryResult.Value.Fields.TryGetNumberOfVotes();
        var numberOfValidVotes = countryResult.Value.Fields.TryGetNumberOfValidVotes();
        var numberOfNullVotes = countryResult.Value.Fields.TryGetNumberOfNullVotes();

        if (turnout == null)
        {
            turnout = Turnout.CreateForRomania(ballot, totalNumberOfEligibleVoters, totalNumberOfVotes, numberOfValidVotes, numberOfNullVotes);
            context.Turnouts.AddAsync(turnout);
        }
        else
        {
            turnout.Update(totalNumberOfEligibleVoters, totalNumberOfVotes, numberOfValidVotes, numberOfNullVotes);
        }
    }

    /// <summary>
    /// this might have a bug 
    /// </summary>
    /// <param name="countries"></param>
    /// <param name="turnout"></param>
    /// <returns></returns>
    private Country? FindCountry(List<Country> countries, TableEntryModel turnout)
    {
        return countries.FirstOrDefault(c => c.Name.EqualsIgnoringAccent(turnout.UatName));
    }

    private CategoryCode MapBallotTypeToCategoryCode(BallotType ballotType)
    {
        switch (ballotType)
        {
            case BallotType.Referendum:
                throw new ArgumentException("Not known yet");

            case BallotType.President:
                throw new ArgumentException("Not known yet");

            case BallotType.Senate:
                throw new ArgumentException("Not known yet");

            case BallotType.House:
                throw new ArgumentException("Not known yet");

            case BallotType.LocalCouncil:
                return CategoryCode.CL;

            case BallotType.CountyCouncil:
                return CategoryCode.CJ;

            case BallotType.Mayor:
                return CategoryCode.P;

            case BallotType.EuropeanParliament:
                return CategoryCode.EUP;

            case BallotType.CountyCouncilPresident:
                return CategoryCode.P;

            case BallotType.CapitalCityMayor:
                throw new ArgumentException("Not known yet");

            case BallotType.CapitalCityCouncil:
                throw new ArgumentException("Not known yet");

            default:
                throw new ArgumentOutOfRangeException(nameof(ballotType), ballotType, null);
        }
    }
}