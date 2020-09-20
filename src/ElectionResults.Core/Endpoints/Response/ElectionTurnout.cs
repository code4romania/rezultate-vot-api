using System.Collections.Generic;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionTurnout
    {
        public int EligibleVoters { get; set; }

        public int TotalVotes { get; set; }

        public List<ElectionTurnoutBreakdown> Breakdown { get; set; }
    }
}