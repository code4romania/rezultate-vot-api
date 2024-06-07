namespace ElectionResults.API.Options;

public class MemoryCacheSettings
{
    public const string SectionKey = "MemoryCacheSettings";

    public int ResultsCacheInMinutes { get; set; }
}