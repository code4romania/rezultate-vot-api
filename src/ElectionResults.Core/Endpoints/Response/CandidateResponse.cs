namespace ElectionResults.Core.Endpoints.Response
{
    public class CandidateResponse
    {
        public string Name { get; set; }

        public string ShortName { get; set; }

        public string PartyColor { get; set; }

        public string PartyLogo { get; set; }

        public int Votes { get; set; }

        public int? Seats { get; set; }

        public int? SeatsGained { get; set; }
        public int TotalSeats { get; set; }
    }
}