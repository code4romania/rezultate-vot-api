using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionScope
    {
        public ElectionDivision Type { get; set; }

        public string CountyName { get; set; }
        public string CountryName { get; set; }

        public string LocalityName { get; set; }

    }
}