namespace ElectionResults.Hangfire.Options;

public class RoAepOptions
{
    public const string SectionKey = "RoAep";

    public string ApiUrl { get; set; }
    public string[] ElectionRoundKeys { get; set; }
}