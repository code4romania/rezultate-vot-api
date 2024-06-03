namespace ElectionResults.Hangfire.Options;

public class HangfireDashboardOptions
{
    public const string SectionKey = "HangfireDashboard";

    public bool IsSecuredDashboard { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
}