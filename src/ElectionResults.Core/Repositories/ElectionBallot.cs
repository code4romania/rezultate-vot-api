using System.Collections.Generic;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public class ElectionBallot
    {
        public int ElectionId { get; set; }

        public List<Ballot> Ballots { get; set; }

        public string ElectionName { get; set; }
    }
}