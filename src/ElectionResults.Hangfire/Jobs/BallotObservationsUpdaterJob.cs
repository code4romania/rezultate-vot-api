using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;
using ElectionResults.Hangfire.Apis.VoteMonitor;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Hangfire.Jobs;

public class BallotObservationsUpdaterJob(IVoteMonitorApi api, ApplicationDbContext dbContext)
{
    public async Task Run(int electionRoundId, CancellationToken ct)
    {
        var voteMonitoringStats = await api.GetStatistics();
        var ballots = await dbContext.Ballots.Where(b => b.ElectionId == electionRoundId).ToListAsync();

        foreach (var ballot in ballots)
        {
            var statistics = voteMonitoringStats.Statistics;

            var observation = await dbContext.Observations.FirstOrDefaultAsync(o => o.BallotId == ballot.BallotId, cancellationToken: ct);
            if (observation == null)
            {
                dbContext.Observations.Add(new Observation
                {
                    BallotId = ballot.BallotId,
                    MessageCount = int.Parse(statistics[0].Value),
                    CoveredPollingPlaces = int.Parse(statistics[1].Value),
                    CoveredCounties = int.Parse(statistics[2].Value),
                    IssueCount = int.Parse(statistics[5].Value),
                    ObserverCount = int.Parse(statistics[4].Value)
                });
            }
            else
            {
                observation.BallotId = ballot.BallotId;
                observation.MessageCount = int.Parse(statistics[0].Value);
                observation.CoveredPollingPlaces = int.Parse(statistics[1].Value);
                observation.CoveredCounties = int.Parse(statistics[2].Value);
                observation.IssueCount = int.Parse(statistics[3].Value);
                observation.ObserverCount = int.Parse(statistics[4].Value);

                dbContext.Observations.Update(observation);
            }
        }
    }
}