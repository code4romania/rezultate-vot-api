using ElectionResults.Core.Endpoints.Response;

namespace ElectionResults.Hangfire.Options;

public class ElectionRoundConfig
{
    public string Key { get; set; }
    public bool HasDiaspora { get; set; }
    public string CronExpression { get; set; }
    public ElectionCategory Category { get; set; }
    public int ElectionRoundId { get; set; }
}

public class CrawlerOptions
{
    public const string SectionKey = "Crawler";

    public string ApiUrl { get; set; }
    public ElectionRoundConfig[] ElectionRounds { get; set; }
    public string VoteMonitorUrl { get; set; }
    public Guid ElectionRoundId { get; set; }
    public string ApiKey { get; set; }
}

