namespace ElectionResults.Core.Endpoints.Response
{
    public class Winner
    {
        public string Name { get; set; }

        public string ShortName { get; set; }

        public string PartyColor { get; set; }

        public int Votes { get; set; }
    }
}