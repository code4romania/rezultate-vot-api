using System.Collections.Generic;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionTurnoutBreakdown
    {
        public TurnoutType Type { get; set; }
        public int Total { get; set; }

        public List<TurnoutCategory> Categories { get; set; }
    }
}