using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using ElectionResults.Core.Endpoints.Response;

namespace ElectionResults.Core.Entities
{
    [Table("elections")]
    public class Election
    {
        [Key]
        public int ElectionId { get; set; }

        public ElectionCategory Category { get; set; }

        public string Name { get; set; }

        public string Subtitle { get; set; }

        public List<Ballot> Ballots { get; set; }

        public DateTime Date { get; set; }
    }
}