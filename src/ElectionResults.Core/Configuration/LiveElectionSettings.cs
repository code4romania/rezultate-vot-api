namespace ElectionResults.Core.Configuration
{
    public class LiveElectionSettings
    {
        public string ResultsType { get; set; }

        public string FtpUser { get; set; }

        public string FtpPassword { get; set; }
        
        public string TurnoutUrl { get; set; }
    }
}