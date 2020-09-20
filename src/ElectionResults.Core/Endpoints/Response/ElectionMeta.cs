using System;

namespace ElectionResults.Core.Endpoints.Response
{
    public class ElectionMeta
    {
        public DateTime Date { get; set; }

        public string Title { get; set; }

        public string Subtitle { get; set; }

        public BallotType Type { get; set; }

        public string Ballot { get; set; }

        public int ElectionId { get; set; }

        public int BallotId { get; set; }
        public int? Round { get; set; }
    }
}