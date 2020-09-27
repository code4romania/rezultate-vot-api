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

        public string GetCacheKey()
        {
            return $"{BallotId}-{CountryId}-{LocalityId}-{CountyId}-{Division}";
        }

        public double GetCacheDurationInMinutes()
        {
            if (BallotId >= 95 && BallotId <= 98)
                return 1;
            return 30;
        }
    }
}