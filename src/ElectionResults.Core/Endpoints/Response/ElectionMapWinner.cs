namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionMapWinner
    {
        public int Id { get; set; }

        public int ValidVotes { get; set; }

        public MapWinner Winner { get; set; }
    }
}