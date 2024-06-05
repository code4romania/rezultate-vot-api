namespace ElectionResults.Hangfire.Apis.VoteMonitor
{
    public class VoteMonitoringStatsModel
    {
        public long Timestamp { get; set; }

        public List<MonitoringInfoModel> Statistics { get; set; }

        public string ElectionName { get; set; }
    }
}