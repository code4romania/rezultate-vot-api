using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public class MemoryCache
    {
        public static readonly CacheSettings Parties = new CacheSettings("all_parties", 60*24);

        public static readonly CacheSettings Ballots = new CacheSettings("all_ballots", 60*24);
        
        public static readonly CacheSettings Elections = new CacheSettings("all_elections", 60*24);
        
        public static readonly CacheSettings Counties = new CacheSettings("all_counties", 60*24);
        
        public static readonly CacheSettings Localities = new CacheSettings("all_localities", 60*24);
        
        public static readonly CacheSettings Locality = new CacheSettings("locality", 60*24);
        
        public static readonly CacheSettings County = new CacheSettings("county", 60*24);
        
        public static readonly CacheSettings Countries = new CacheSettings("all_countries", 60*24);

        public static string CreateWinnersKey(int ballotId, int? countyId, ElectionDivision division)
        {
            return $"winner-{ballotId}-{countyId}-{division}";
        }
    }

    public class CacheSettings
    {
        public CacheSettings(string key, int durationInMinutes)
        {
            Key = key;
            Minutes = durationInMinutes;
        }

        public string Key { get; set; }

        public int Minutes { get; set; }
    }
}
