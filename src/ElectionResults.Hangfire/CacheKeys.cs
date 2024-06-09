using Microsoft.Extensions.Caching.Memory;

namespace ElectionResults.Hangfire;

public class CacheKeys
{
    public const string Countries = "Countries";
    public const string RoCounties = "Counties";
    public const string RoLocalities = "Localities";
    public const string RoParties = "Parties";
}
