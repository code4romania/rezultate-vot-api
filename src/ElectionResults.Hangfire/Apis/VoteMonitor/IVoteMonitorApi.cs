namespace ElectionResults.Hangfire.Apis.VoteMonitor;

public interface IVoteMonitorApi
{
    Task<VoteMonitoringStatsModel> GetStatistics();
}