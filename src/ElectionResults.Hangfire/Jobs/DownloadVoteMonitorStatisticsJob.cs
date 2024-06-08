using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.VoteMonitor;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Hangfire.Jobs;

public class DownloadVoteMonitorStatisticsJob(IVoteMonitorApi api, ApplicationDbContext dbContext)
{
    public async Task Run(List<int> electionRoundIds, Guid voteMonitorElectionRoundId, CancellationToken ct)
    {
        var voteMonitoringStats = await api.GetStatistics(voteMonitorElectionRoundId);

        foreach (var electionRoundId in electionRoundIds)
        {
            var ballots = await dbContext.Ballots.Where(b => b.ElectionId == electionRoundId).ToListAsync(ct);

            foreach (var ballot in ballots)
            {
                var observation = await dbContext
                    .Observations
                    .Where(o => o.BallotId == ballot.BallotId)
                    .FirstOrDefaultAsync(ct);
                
                // during this elections Romanian counties are on level2
                if (observation == null)
                {
                    dbContext.Observations.Add(Observation.Create(ballot, voteMonitoringStats.StartedForms,
                        voteMonitoringStats.VisitedPollingStations, voteMonitoringStats.Level2Visited,
                        voteMonitoringStats.FlaggedAnswers, voteMonitoringStats.Observers));
                }
                else
                {
                    observation.Update(voteMonitoringStats.StartedForms, voteMonitoringStats.VisitedPollingStations,
                        voteMonitoringStats.Level2Visited, voteMonitoringStats.FlaggedAnswers,
                        voteMonitoringStats.Observers);
                }
            }
        }

        await dbContext.SaveChangesAsync(ct);
    }
}