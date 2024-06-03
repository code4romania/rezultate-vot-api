namespace ElectionResults.Hangfire.Jobs;

public interface ICheckStaticDataJob
{
    Task Run(string electionRoundId, CancellationToken ct = default);
}