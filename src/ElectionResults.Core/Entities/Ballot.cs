using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectionResults.Core.Endpoints.Response;

namespace ElectionResults.Core.Entities
{
    [Table("ballots")]
    public class Ballot
    {
        [Key]
        public int BallotId { get; set; }

        public string Name { get; set; }

        public string Subtitle { get; set; }

        public BallotType BallotType { get; set; }

        [ForeignKey(nameof(Entities.Turnout))]
        public int? TurnoutId { get; set; }

        [ForeignKey(nameof(Entities.Election))]
        public int ElectionId { get; set; }

        public int? Round { get; set; }

        public Turnout Turnout { get; set; }

        public DateTime Date { get; set; }
    }
}