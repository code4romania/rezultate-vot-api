namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionObservation
    {
        public int CoveredPollingPlaces { get; set; }
        public int CoveredCounties { get; set; }
        public int ObserverCount { get; set; }
        public int MessageCount { get; set; }
        public int IssueCount { get; set; }
    }
}