namespace ElectionResults.Hangfire.Jobs;

public class Vote
{
    public string Candidate { get; set; }
    public string Party { get; set; }
    public int Votes { get; set; }

    public int Mandates1 { get; set; }

    public int Mandates2 { get; set; }
}