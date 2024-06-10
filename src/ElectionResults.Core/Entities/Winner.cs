using ElectionResults.Core.Endpoints.Response;
using System;
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


        public static Winner CreateLocalityWinner(int ballotId,
            int countyId,
            int localityId,
            CandidateResult localityWinner,
            Turnout turnoutForLocality)
        {
            var winner = new Winner
            {
                BallotId = ballotId,
                CandidateId = localityWinner.Id,
                CountyId = countyId,
                LocalityId = localityId,
                Division = ElectionDivision.Locality,
                Name = localityWinner.Name,
                PartyId = localityWinner.PartyId,
                TurnoutId = turnoutForLocality?.Id,
            };

            return winner;
        }  
        
        public static Winner CreateForDiasporaCountry(int ballotId,
            int countryId,
            CandidateResult localityWinner,
            Turnout turnoutForLocality,
            int votes)
        {
            var winner = new Winner
            {
                BallotId = ballotId,
                CandidateId = localityWinner.Id,
                CountryId = countryId,
                Division = ElectionDivision.Diaspora_Country,
                Name = localityWinner.Name,
                PartyId = localityWinner.PartyId,
                TurnoutId = turnoutForLocality?.Id,
                Votes = votes,

            };

            return winner;
        }

        internal static Winner CreateForCounty(Ballot ballot, CandidateResult countyWinner,
            ElectionMapWinner electionMapWinner, int turnoutId, int countyId)
        {
            return new Winner
            {
                BallotId = ballot.BallotId,
                Ballot = ballot,
                CandidateId = countyWinner.Id,
                Name = electionMapWinner.Winner.Name,
                PartyId = electionMapWinner.Winner.Party?.Id,
                Votes = electionMapWinner.Winner.Votes,
                TurnoutId = turnoutId,
                CountyId = countyId,
                Division = ElectionDivision.County
            };
        }
    }
}
