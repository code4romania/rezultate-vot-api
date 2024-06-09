namespace ElectionResults.Hangfire.Apis;

public interface ITurnoutCrawler
{
    Task InsertEuroTurnouts();
}