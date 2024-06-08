using CsvHelper;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.RoAep;
using ElectionResults.Hangfire.Apis.RoAep.SicpvModels;
using ElectionResults.Hangfire.Apis.RoAep.SimpvModels;
using ElectionResults.Hangfire.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Globalization;
using Z.EntityFramework.Plus;

namespace ElectionResults.Hangfire.Jobs;
public class SeedData(IRoAepApi api, ApplicationDbContext context, ILogger<SeedData> logger)
{
    public async Task Run(CancellationToken ct = default)
    {
        var existingCounties = (await context.Counties.FromCacheAsync(ct, CacheKeys.RoCounties)).ToList();
        var existingLocalities = (await context.Localities.FromCacheAsync(ct, CacheKeys.RoLocalities)).ToList();  
        
        var existingCountries = (await context.Countries.FromCacheAsync(ct, CacheKeys.Countries)).ToList();

        var allUats = new List<(bool resolved, string countyName, string localityName, int CountyId, int LocalityId)>();

        var counties = await api.ListCounties("europarlamentare09062024");

        foreach (var county in counties)
        {
            if (county.Code.InvariantEquals("SR"))
            {
                continue;
            }
            if (county.Code.InvariantEquals("B"))
            {
                var localities = await api.ListLocalities("europarlamentare09062024", county.Code);
                var countyId = existingCounties.First(dbCounty => CountiesAreEqual(dbCounty, county)).CountyId;

                foreach (var locality in localities)
                {
                    var result = CheckLocality(county, existingLocalities.Where(l => l.CountyId == countyId).ToList(), locality);
                    allUats.Add(result);
                }
            }
            else
            {
                var uats = await api.ListUats("europarlamentare09062024", county.Code);
                logger.LogWarning(county.Code);
                var countyId = existingCounties.First(dbCounty => CountiesAreEqual(dbCounty, county)).CountyId;

                foreach (var uat in uats)
                {
                    var result = CheckUat(county, existingLocalities.Where(l => l.CountyId == countyId).ToList(), uat);
                    allUats.Add(result);
                }
            }
        }

        var euroParlamentareBallot = await context
            .Ballots
            .Where(b => b.ElectionId == 51)
            .FirstAsync();

        var localeBallots = await context
        .Ballots
        .Where(b => b.ElectionId == 50)
        .ToListAsync();

        var parties = await context.Parties.ToListAsync();

        //await ImportDateForLocale(parties, ballots, allUats);
        await ImportDateForEuroParlamentare(parties, euroParlamentareBallot, allUats, existingCountries);
    }

    private async Task ImportDateForLocale(List<Party> parties, List<Ballot> ballots, List<(bool resolved, string countyName, string localityName, int CountyId, int LocalityId)> allUats )
    {

        using (var reader = new StreamReader("candidati_locale_07.06.2024.csv"))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();

            var records = csv.GetRecords<LocaleCsvModel>().ToList();

            var candidateResults = new List<CandidateResult>();

            foreach (var record in records)
            {
                try
                {
                    var ballot = ballots.First(b => b.BallotType == MapBallotType(record.TP));
                    int countyId = 0;
                    if (countyCache.ContainsKey(record.Judet))
                    {
                        countyId = countyCache[record.Judet];
                    }
                    else
                    {
                        countyId = allUats.First(x => x.countyName == record.Judet).CountyId;
                        countyCache.Add(record.Judet, countyId);
                    }
                    int? localityId = null;
                    if (localityCache.ContainsKey(record.Judet.GenerateSlug() + "-" + record.UAT.GenerateSlug()))
                    {
                        localityId = localityCache[record.Judet.GenerateSlug() + "-" + record.UAT.GenerateSlug()];
                    }
                    else
                    {
                        localityId = string.IsNullOrEmpty(record.UAT) ? null : allUats.First(x => x.countyName.InvariantEquals(record.Judet) && x.localityName.InvariantEquals(record.UAT)).LocalityId;

                        localityCache.Add(record.Judet.GenerateSlug() + "-" + record.UAT.GenerateSlug(), localityId);
                    }



                    var canditateResult = CreateCandidateResult(record, ballot, parties, localityId, countyId, ElectionDivision.Locality);

                    candidateResults.Add(canditateResult);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed processing {@record}", record);
                }
            }

            await context.BulkInsertAsync(candidateResults);
        }
    }
    
    private async Task ImportDateForEuroParlamentare(List<Party> parties,
        Ballot ballot,
        List<(bool resolved, string countyName, string localityName, int CountyId, int LocalityId)> allUats,
        List<Country> countries)
    {
        using (var reader = new StreamReader("candidati_euro_07.06.2024.csv"))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
            csv.Read();
            csv.ReadHeader();

            var records = csv.GetRecords<EuroParlamentareCsvModel>().ToList();

            var candidateResults = new List<CandidateResult>();

            foreach (var record in records)
            {
                try
                {
                    int countyId = 0;
                    foreach (var uat in allUats)
                    {
                        var canditateResult = CreateCandidateResult(record, ballot, parties, uat.LocalityId, uat.CountyId, ElectionDivision.Locality);

                        candidateResults.Add(canditateResult);
                    }

                    foreach(var country in countries)
                    {
                        var canditateResult = CreateCandidateResult(record, ballot, parties, country.Id);

                        candidateResults.Add(canditateResult);
                    }

                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Failed processing {@record}", record);
                }
            }

            await context.BulkInsertAsync(candidateResults);
        }
    }

    private BallotType MapBallotType(string tip)
    {
        if (tip.Trim().InvariantEquals("CONSILIERI LOCALI"))
        {
            return BallotType.LocalCouncil;
        }

        if (tip.Trim().InvariantEquals("CONSILIU JUDEŢEAN"))
        {
            return BallotType.CountyCouncil;
        }

        if (tip.Trim().InvariantEquals("PRESEDINTE CONSILIU JUDEŢEAN"))
        {
            return BallotType.CountyCouncilPresident;
        }

        if (tip.Trim().InvariantEquals("PRIMARI"))
        {
            return BallotType.Mayor;
        }

        if (tip.Trim().InvariantEquals("CONSILIU GENERAL"))
        {
            return BallotType.CountyCouncil;
        }

        if (tip.Trim().InvariantEquals("PRIMAR GENERAL"))
        {
            return BallotType.Mayor;
        }

        throw new Exception("Unknown");
    }
    private static Dictionary<string, Party> partyCache = new Dictionary<string, Party>();
    private static Dictionary<string, int> countyCache = new Dictionary<string, int>();
    private static Dictionary<string, int?> localityCache = new Dictionary<string, int?>();

    private static CandidateResult CreateCandidateResult(LocaleCsvModel record, Ballot ballot, List<Party> parties,
           int? localityId, int countyId, ElectionDivision division = ElectionDivision.Locality)
    {
        var candidateResult = new CandidateResult
        {
            BallotId = ballot.BallotId,
            Division = division,
            Name = record.NP,
            CountyId = countyId,
            LocalityId = localityId,
            BallotPosition = record.PL,
            PartyName = record.Partid,
        };
        if (partyCache.ContainsKey(record.Partid))
        {
            var party = partyCache[record.Partid];

            candidateResult.PartyId = party.Id;
            candidateResult.PartyName = party.Name;
            candidateResult.ShortName = party.ShortName;
        }
        else
        {
            var party = parties.FirstOrDefault(p => p.Alias.ContainsString(record.Partid))
                                     ?? parties.FirstOrDefault(p => p.Name.ContainsString(record.Partid));
            if (party is not null)
            {
                candidateResult.PartyId = party.Id;
                candidateResult.PartyName = party.Name;
                candidateResult.ShortName = party.ShortName;

                partyCache.Add(record.Partid, party);
            }
        }

        return candidateResult;
    }  
    
    private static CandidateResult CreateCandidateResult(EuroParlamentareCsvModel record, Ballot ballot, List<Party> parties,
           int? localityId, int countyId, ElectionDivision division = ElectionDivision.Locality)
    {
        var candidateResult = new CandidateResult
        {
            BallotId = ballot.BallotId,
            Division = division,
            Name = record.NP,
            CountyId = countyId,
            LocalityId = localityId,
            BallotPosition = record.PL,
            PartyName = record.Partid,
        };
        if (partyCache.ContainsKey(record.Partid))
        {
            var party = partyCache[record.Partid];

            candidateResult.PartyId = party.Id;
            candidateResult.PartyName = party.Name;
            candidateResult.ShortName = party.ShortName;
        }
        else
        {
            var party = parties.FirstOrDefault(p => p.Alias.ContainsString(record.Partid))
                                     ?? parties.FirstOrDefault(p => p.Name.ContainsString(record.Partid));
            if (party is not null)
            {
                candidateResult.PartyId = party.Id;
                candidateResult.PartyName = party.Name;
                candidateResult.ShortName = party.ShortName;

                partyCache.Add(record.Partid, party);
            }
        }

        return candidateResult;
    }

    private static CandidateResult CreateCandidateResult(EuroParlamentareCsvModel record, Ballot ballot, List<Party> parties, int countryId)
    {
        var candidateResult = new CandidateResult
        {
            BallotId = ballot.BallotId,
            Name = record.NP,
            CountryId = countryId,
            BallotPosition = record.PL,
            PartyName = record.Partid,
            Division = ElectionDivision.Diaspora_Country
        };

        if (partyCache.ContainsKey(record.Partid))
        {
            var party = partyCache[record.Partid];

            candidateResult.PartyId = party.Id;
            candidateResult.PartyName = party.Name;
            candidateResult.ShortName = party.ShortName;
        }
        else
        {
            var party = parties.FirstOrDefault(p => p.Alias.ContainsString(record.Partid))
                                     ?? parties.FirstOrDefault(p => p.Name.ContainsString(record.Partid));
            if (party is not null)
            {
                candidateResult.PartyId = party.Id;
                candidateResult.PartyName = party.Name;
                candidateResult.ShortName = party.ShortName;

                partyCache.Add(record.Partid, party);
            }
        }

        return candidateResult;
    }

    private static bool CountiesAreEqual(County dbCounty, CountyModel county)
    {
        if (county.Code.InvariantEquals("B"))
        {
            return dbCounty.ShortName.InvariantEquals(county.Code);
        }

        return dbCounty.Name.InvariantEquals(county.Name) && dbCounty.ShortName.InvariantEquals(county.Code);
    }

    private (bool resolved, string countyName, string localityName, int CountyId, int LocalityId) CheckUat(CountyModel county, List<Locality> existingUats, UatModel uat)
    {
        var dbLocality = existingUats.FirstOrDefault(bdUat => bdUat.Siruta == uat.Siruta);

        if (dbLocality is null)
        {
            logger.LogWarning("{countyCode} - UAT not exists {@uat}", county.Code, uat);
            return (false, default, default, default, default);
        }

        return (true, county.Name, uat.Name, dbLocality.CountyId, dbLocality.LocalityId);
    }

    private (bool resolved, string countyName, string localityName, int CountyId, int LocalityId) CheckLocality(CountyModel county, List<Locality> existingLocalities, Apis.RoAep.SicpvModels.LocalityModel locality)
    {
        var dbLocality = existingLocalities.FirstOrDefault(dbLocality => dbLocality.Siruta.ToString() == locality.Code);

        if (dbLocality is null)
        {
            logger.LogWarning("{countyCode} - UAT not exists {@uat}", county.Code, locality);
            return (false, default, default, default, default);
        }

        return (true, county.Name, locality.Name, dbLocality.CountyId, dbLocality.LocalityId);
    }
}