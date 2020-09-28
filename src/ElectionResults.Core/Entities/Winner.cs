using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    public class Winner
    {
        [Key]
        public int Id { get; set; }


        public string Name { get; set; }

        public int Votes { get; set; }

        [ForeignKey(nameof(Entities.CandidateResult))]
        public int? CandidateId { get; set; }

        public CandidateResult Candidate { get; set; }

        [ForeignKey(nameof(Entities.Party))]
        public int? PartyId { get; set; }

        public Party Party { get; set; }

        [ForeignKey(nameof(Ballot))]
        public int? BallotId { get; set; }

        [ForeignKey(nameof(TurnoutId))]
        public int? TurnoutId { get; set; }

        public Turnout Turnout { get; set; }

        public ElectionDivision Division { get; set; }

        [ForeignKey(nameof(County))]
        public int? CountyId { get; set; }

        [ForeignKey(nameof(Country))]
        public int? CountryId { get; set; }

        [ForeignKey(nameof(Locality))]
        public int? LocalityId { get; set; }

        public Ballot Ballot { get; set; }
    }
}
