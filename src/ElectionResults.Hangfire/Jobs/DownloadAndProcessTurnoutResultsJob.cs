using System.Collections.Concurrent;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Extensions;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.RoAep;
using ElectionResults.Hangfire.Apis.RoAep.SicpvModels;
using Microsoft.EntityFrameworkCore;
using ElectionResults.Hangfire.Extensions;
using Z.EntityFramework.Plus;
using System.Text;

namespace ElectionResults.Hangfire.Jobs;

public class DownloadAndProcessTurnoutResultsJob(IRoAepApi roAepApi,
    ApplicationDbContext context,
    ILogger<DownloadAndProcessTurnoutResultsJob> logger)
{
    private readonly ConcurrentBag<CandidateResult> _candidates = new();
    private const string DiasporaCountyCode = "SR";

    public async Task Run(string electionRoundKey, int electionRoundId, bool hasDiaspora, StageCode stageCode)
    {
        var electionRound = context.Elections.FirstOrDefault(x => x.ElectionId == electionRoundId);
        if (electionRound == null)
        {
            throw new ArgumentException($"Election round {electionRoundId} does not exist!");
        }
        await context.Database.ExecuteSqlRawAsync("CREATE TEMPORARY TABLE tempcandidateresults LIKE candidateresults;");

        var counties = (await context.Counties.FromCacheAsync(CacheKeys.RoCounties)).ToList();
        var countries = (await context.Countries.FromCacheAsync(CacheKeys.Countries)).ToList();
        var localities = (await context.Localities.FromCacheAsync(CacheKeys.RoLocalities)).ToList();
        var parties = (await context.Parties.FromCacheAsync(CacheKeys.RoParties)).ToList();

        var ballots = await context
            .Ballots
            .Where(b => b.ElectionId == electionRound.ElectionId)
            .ToListAsync();

        var countiesResults = new ConcurrentDictionary<string, Dictionary<ScopeCode, ScopeModel>>();
        await Parallel.ForEachAsync(counties, async (county, token) =>
        {
            try
            {
                var countyResult = await roAepApi.GetPVForCounty(electionRoundKey, county.ShortName, stageCode);
                var (hasValue, stage) = GetStageScopeData(electionRoundKey, stageCode, countyResult, county.ShortName);

                if (hasValue)
                {
                    countiesResults.TryAdd(county.ShortName, stage);
                }
            }
            catch (Exception e)
            {
                logger.LogError(e, "Error while downloading json for county {county}", county.ShortName);
            }
        });

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

        var turnouts = await context
            .Turnouts.ToListAsync();

        Parallel.ForEach(ballots, parallelOptions: new ParallelOptions{MaxDegreeOfParallelism = 1}, (ballot) =>
        {
            var turnoutsForBallot = turnouts
                .Where(t => t.BallotId == ballot.BallotId).ToList();

            if (hasDiaspora && diasporaResult is not null)
            {
                UpdateDiasporaTurnouts(countries, ballot, diasporaResult, turnoutsForBallot);
            }

            foreach (var countyResult in countiesResults)
            {
                var county = counties.First(x => x.ShortName == countyResult.Key);
                var countyLocalities = localities.Where(x => x.CountyId == county.CountyId).ToList();
                if (countyResult.Value == null)
                {
                    logger.LogWarning("No data for {county}", county.Name);
                    continue;
                }
                UpdateCountyTurnout(countyResult.Value[ScopeCode.CNTY].Categories, ballot, turnoutsForBallot, county);
                UpdateLocalitiesTurnouts(countyResult.Value[ScopeCode.UAT].Categories, ballot, turnoutsForBallot, county, countyLocalities);


                List<KeyValuePair<string, TableEntryModel>> list = new List<KeyValuePair<string, TableEntryModel>>();
                if (ballot.BallotType == BallotType.LocalCouncil)
                    list = countyResult.Value[ScopeCode.UAT].Categories[CategoryCode.CL].GetTable().OrderBy(c => c.Value.UatName).ToList();
                else if (ballot.BallotType == BallotType.Mayor)
                    list = countyResult.Value[ScopeCode.UAT].Categories[CategoryCode.P].GetTable().OrderBy(c => c.Value.UatName).ToList();
                else if (ballot.BallotType == BallotType.CountyCouncil)
                    list = countyResult.Value[ScopeCode.CNTY].Categories[CategoryCode.CJ].GetTable().OrderBy(c => c.Value.UatName).ToList();
                else if (ballot.BallotType == BallotType.CountyCouncilPresident)
                    list = countyResult.Value[ScopeCode.CNTY].Categories[CategoryCode.PCJ].GetTable().OrderBy(c => c.Value.UatName).ToList();

                if ((ballot.BallotType == BallotType.CountyCouncilPresident ||
                     ballot.BallotType == BallotType.CountyCouncil) && list.Any())
                {
                    var jsonCounty = list.FirstOrDefault(l => l.Value.CountyCode.ToLower() == county.ShortName.ToLower());
                    UpdateCountyCandidates(jsonCounty, county, ballot, parties);
                }

                if (ballot.BallotType == BallotType.Mayor || ballot.BallotType == BallotType.LocalCouncil)
                {
                    foreach (var jsonLocality in list)
                    {
                        UpdateLocalityCandidates(localities, jsonLocality, county, ballot,
                            parties);
                    }
                }
            }

            var firstResult = countiesResults.Values.FirstOrDefault();
            if (firstResult is not null && firstResult.ContainsKey(ScopeCode.CNTRY))
            {
                UpdateNationalTurnout(firstResult[ScopeCode.CNTRY].Categories, ballot, turnoutsForBallot);
            }
            Console.WriteLine($"Finished ballot {ballot.Name}");
        });

        await context.SaveChangesAsync();

        foreach (var ballot in ballots)
        {
            await context.Database.ExecuteSqlRawAsync($@"
                DELETE FROM winners WHERE ballotId = {ballot.BallotId};
                DELETE FROM candidateresults WHERE ballotId = {ballot.BallotId};
            ");
            var candidateResults = _candidates.Where(c => c.BallotId == ballot.BallotId).ToList();
            await BulkInsertCandidateResultsAsync(candidateResults);
            await context.Database.ExecuteSqlRawAsync($@"
            INSERT INTO candidateresults (Votes, BallotId, Name, ShortName, PartyName, PartyId, YesVotes, NoVotes, SeatsGained, Division, CountyId, LocalityId, TotalSeats, Seats1, Seats2, OverElectoralThreshold, CountryId, BallotPosition)
            SELECT Votes, BallotId, Name, ShortName, PartyName, PartyId, YesVotes, NoVotes, SeatsGained, Division, CountyId, LocalityId, TotalSeats, Seats1, Seats2, OverElectoralThreshold, CountryId, BallotPosition
            FROM TempCandidateResults where ballotid = {ballot.BallotId};
        ");
            
        }
    }

    private async Task BulkInsertCandidateResultsAsync(List<CandidateResult> candidateResults)
    {
        if (candidateResults == null || !candidateResults.Any())
        {
            return;
        }

        var insertQuery = new StringBuilder();
        insertQuery.Append("INSERT INTO TempCandidateResults (Votes, BallotId, Name, ShortName, PartyName, PartyId, YesVotes, NoVotes, SeatsGained, Division, CountyId, LocalityId, TotalSeats, Seats1, Seats2, OverElectoralThreshold, CountryId, BallotPosition) VALUES ");

        var valueQueries = new List<string>();
        foreach (var candidate in candidateResults.Where(c => c != null))
        {
            valueQueries.Add($"({candidate.Votes}, {candidate.BallotId}, '{EscapeSql(candidate.Name)}', '{EscapeSql(candidate.ShortName)}', '{EscapeSql(candidate.PartyName)}', {candidate.PartyId?.ToString() ?? "NULL"}, {candidate.YesVotes}, {candidate.NoVotes}, {candidate.SeatsGained}, {(int)candidate.Division}, {candidate.CountyId?.ToString() ?? "NULL"}, {candidate.LocalityId?.ToString() ?? "NULL"}, {candidate.TotalSeats}, {candidate.Seats1}, {candidate.Seats2}, {(candidate.OverElectoralThreshold ? 1 : 0)}, {candidate.CountryId?.ToString() ?? "NULL"}, {candidate.BallotPosition})");
        }

        insertQuery.Append(string.Join(", ", valueQueries));
        insertQuery.Append(";");

        await context.Database.ExecuteSqlRawAsync(insertQuery.ToString());
    }

    private string EscapeSql(string input)
    {
        if (input.IsEmpty())
            return string.Empty;
        return input.Replace("'", "''");
    }

    private void UpdateLocalityCandidates(List<Locality> localities, KeyValuePair<string, TableEntryModel> jsonLocality, County county, Ballot ballot, List<Party> parties)
    {
        var locality =
            localities.FirstOrDefault(l => l.Siruta == int.Parse(jsonLocality.Value.UatSiruta));
        if (locality == null)
        {
            logger.LogWarning("Locality {locality} not found in the database", jsonLocality.Value.UatName);
        }
        var newResults = jsonLocality.Value.Votes.Select(r => CreateCandidateResult(r, ballot, parties, locality.LocalityId, locality.CountyId))
            .ToList();
        foreach (var candidateResult in newResults)
        {
            _candidates.Add(candidateResult);
        }
    }
    private void UpdateCountyCandidates(KeyValuePair<string, TableEntryModel> jsonLocality, County county, Ballot ballot, List<Party> parties)
    {
        var newResults = jsonLocality.Value.Votes.Select(r => CreateCandidateResult(r, ballot, parties, null, county.CountyId, ElectionDivision.County))
            .ToList();

        foreach (var candidateResult in newResults)
        {
            _candidates.Add(candidateResult);
        }
    }
    private static CandidateResult CreateCandidateResult(VoteModel vote, Ballot ballot, List<Party> parties,
        int? localityId, int? countyId, ElectionDivision division = ElectionDivision.Locality)
    {
        var candidateResult = new CandidateResult
        {
            BallotId = ballot.BallotId,
            Division = division,
            Votes = (int)vote.Votes,
            Name = vote.Candidate,
            CountyId = countyId,
            LocalityId = localityId,
            Seats1 = vote.Mandates1,
            Seats2 = vote.Mandates2
        };
        var partyName = ballot.BallotType == BallotType.LocalCouncil || ballot.BallotType == BallotType.CountyCouncil ? vote.Candidate : vote.Party;
        candidateResult.PartyId = parties.FirstOrDefault(p => p.Alias.GenerateSlug().ContainsString(partyName.GenerateSlug()))?.Id
                                  ?? parties.FirstOrDefault(p => p.Name.GenerateSlug().ContainsString(partyName.GenerateSlug()))?.Id;
       
        return candidateResult;
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

        foreach (var turnout in resultsCategory!.GetTable().Values)
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

        if (!uatsResults.ContainsKey(category) || uatsResults[category].Table == null)
        {
            logger.LogWarning("Could not find requested {category} {ballotType} in response for {countyCode}", category, ballot.BallotType, county.ShortName);
            return;
        }

        foreach (var uatTurnout in uatsResults[category].GetTable())
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
        if (!countyResults[category].GetTable().Any())
        {
            logger.LogWarning("No data for {category} {ballotType} in response for {countyCode}",
                category, ballot.BallotType, county.ShortName);
            return;
        }

        var countyResult = countyResults[category].GetTable().First();

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
        if (!countryData[category].GetTable().Any())
        {
            logger.LogWarning("No data for {category} {ballotType} for Romania",
                category, ballot.BallotType);
            return;
        }

        // When scope is CNTRY we have only one entry  with key = RO
        var countryResult = countryData[category].GetTable().First();

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