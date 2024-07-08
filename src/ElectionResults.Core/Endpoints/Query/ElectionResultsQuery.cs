using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Query
{
    public class ElectionResultsQuery
    {
        public ElectionDivision Division { get; set; }

        public int BallotId { get; set; }

        public int? CountyId { get; set; }

        public int? LocalityId { get; set; }

        public int? Round { get; set; }

        public int? CountryId { get; set; }

        public string GetCacheKey(string route)
        {
            return $"{route}-{BallotId}-{CountryId}-{LocalityId}-{CountyId}-{Division}";
        }
    }
}