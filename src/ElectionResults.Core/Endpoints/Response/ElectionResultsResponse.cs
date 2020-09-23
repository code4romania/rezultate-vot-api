using System.Collections.Generic;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionResultsResponse
    {
        public int? EligibleVoters { get; set; }

        public int? TotalVotes { get; set; }

        public int? VotesByMail { get; set; }

        public int? ValidVotes { get; set; }

        public int? NullVotes { get; set; }

        public int? TotalSeats { get; set; }

        public List<CandidateResponse> Candidates { get; set; }
    }
}