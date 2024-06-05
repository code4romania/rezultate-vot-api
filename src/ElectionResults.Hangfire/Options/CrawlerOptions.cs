using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Hangfire.Apis.RoAep.Models;

namespace ElectionResults.Hangfire.Options;

public class ElectionRoundConfig
{
    public string Key { get; set; }
    public bool HasDiaspora { get; set; }
    public string CronExpression { get; set; }
    public ElectionCategory Category { get; set; }
    public int ElectionRoundId { get; set; }
    public StageCode Stage { get; set; }
}

public class CrawlerOptions
{
    public const string SectionKey = "Crawler";

    public string ApiUrl { get; set; }
    public ElectionRoundConfig[] ElectionRounds { get; set; }
}

