using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionScope
    {
        public ElectionDivision Type { get; set; }

        public string CountyName { get; set; }

        public string CountryName { get; set; }

        public string LocalityName { get; set; }
        
        public int? CountyId { get; set; }
        
        public int? LocalityId { get; set; }

        public int? CountryId { get; set; }
    }
}