using System.Collections.Generic;

namespace ElectionResults.Core.Endpoints.Query
{
    public class VoteMonitoringStats
    {
        public long Timestamp { get; set; }

        public List<MonitoringInfo> Statistics { get; set; }

        public string ElectionName { get; set; }
    }
}