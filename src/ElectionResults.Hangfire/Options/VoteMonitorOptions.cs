namespace ElectionResults.Hangfire.Options;

public class VoteMonitorOptions
{
    public const string SectionKey = "VoteMonitor";

    public string ApiUrl { get; set; }
    public string ApiKey { get; set; }
}