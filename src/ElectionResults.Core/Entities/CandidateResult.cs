using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("candidateresults")]
    public class CandidateResult
    {
        [Key]
        public int Id { get; set; }

        public int Votes { get; set; }

        [ForeignKey(nameof(Ballot))]
        public int BallotId { get; set; }

        public string Name { get; set; }

        public string ShortName { get; set; }

        public string Party { get; set; }

        public int YesVotes { get; set; }

        public int NoVotes { get; set; }

        public ElectionDivision Division { get; set; }

        [ForeignKey(nameof(County))]
        public int? CountyId { get; set; }

        [ForeignKey(nameof(Locality))]
        public int? LocalityId { get; set; }

        public string Color { get; set; }

        public string Logo { get; set; }

        public int TotalSeats { get; set; }

        public int Seats1 { get; set; }

        public int Seats2 { get; set; }

        public bool OverElectoralThreshold { get; set; }
    }
}