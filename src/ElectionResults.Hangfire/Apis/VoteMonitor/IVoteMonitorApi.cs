using Refit;

namespace ElectionResults.Hangfire.Apis.VoteMonitor;

public interface IVoteMonitorApi
{
    [Get("/api/statistics/overview")]
    Task<VoteMonitoringStatsModel> GetStatistics([AliasAs("electionRoundIds")] Guid electionRoundId);
}