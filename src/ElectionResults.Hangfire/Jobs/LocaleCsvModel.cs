namespace ElectionResults.Hangfire.Jobs
{
    internal class LocaleCsvModel
    {
        public string Judet { get; set; }
        public string UAT { get; set; }
        public string TP { get; set; }
        public string Partid { get; set; }
        public string NP { get; set; }
        public int PL { get; set; }
    }    
    
    internal class EuroParlamentareCsvModel
    {
        public string Partid { get; set; }
        public string NP { get; set; }
        public int PL { get; set; }
    }
}