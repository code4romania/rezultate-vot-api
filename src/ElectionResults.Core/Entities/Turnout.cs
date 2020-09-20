using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("turnouts")]
    public class Turnout
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Ballot))]
        public int BallotId { get; set; }

        [ForeignKey(nameof(County))]
        public int? CountyId { get; set; }

        [ForeignKey(nameof(Locality))]
        public int? LocalityId { get; set; }

        public int EligibleVoters { get; set; }

        public int TotalVotes { get; set; }

        public int NullVotes { get; set; }

        public int VotesByMail { get; set; }

        public int ValidVotes { get; set; }

        public int TotalSeats { get; set; }

        public int Coefficient { get; set; }

        public int Threshold { get; set; }

        public int Circumscription { get; set; }

        public int MinVotes { get; set; }

        public ElectionDivision Division { get; set; }

        public int Mandates { get; set; }
    }
}
