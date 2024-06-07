using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("winners")]
    public class Winner : IAmEntity
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


        public static Winner Create(int ballotId,
            int? countyId,
            CandidateResult localityWinner,
            Turnout turnoutForLocality,
            ElectionDivision division)
        {
            var winner = new Winner
            {
                BallotId = ballotId,
                CandidateId = localityWinner.Id,
                CountyId = countyId,
                Division = division,
                Name = localityWinner.Name,
                PartyId = localityWinner.PartyId,
                TurnoutId = turnoutForLocality?.Id,
            };

            return winner;
        }
    }
}
