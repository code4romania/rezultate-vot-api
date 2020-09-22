using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("observations")]
    public class Observation
    {
        [Key]
        public int Id { get; set; }

        [ForeignKey(nameof(Ballot))]
        public int BallotId { get; set; }

        public int CoveredPollingPlaces { get; set; }

        public int CoveredCounties { get; set; }

        public int ObserverCount { get; set; }

        public int MessageCount { get; set; }

        public int IssueCount { get; set; }
    }
}