using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Endpoints.Response
{
    public class MapWinner
    {
        public string Name { get; set; }

        public string ShortName { get; set; }

        public string PartyColor { get; set; }

        public int Votes { get; set; }

        public Party Party { get; set; }
    }
}