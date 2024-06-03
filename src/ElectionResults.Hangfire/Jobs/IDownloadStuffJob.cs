namespace ElectionResults.Hangfire.Jobs;

public interface IDownloadStuffJob
{
    Task Run(string electionRoundId, CancellationToken ct);
}