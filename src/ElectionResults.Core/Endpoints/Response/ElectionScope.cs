using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionScope
    {
        public ElectionDivision Type { get; set; }
        public string County { get; set; }
        public string City { get; set; }

    }
}