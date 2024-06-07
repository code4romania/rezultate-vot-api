using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("partyresults")]
    public class PartyResult : IAmEntity
    {
        public int Id { get; set; }

        [ForeignKey(nameof(Ballot))]
        public int BallotId { get; set; }

        [ForeignKey(nameof(Party))]
        public int PartyId { get; set; }

        public int Votes { get; set; }
    }
}