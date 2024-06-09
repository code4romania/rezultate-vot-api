using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("turnouts")]
    public class Turnout : IAmEntity
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

        public int CorrespondenceVotes { get; set; }
        public int PermanentListsVotes { get; set; }
        public int SpecialListsVotes { get; set; }
        public int SuplimentaryVotes { get; set; }

        [ForeignKey(nameof(Country))]
        public int? CountryId { get; set; }

        [NotMapped]
        public int CountedVotes { get; set; }
        public static Turnout CreateForDiasporaCountry(Ballot ballot,
            Country country,
            int totalNumberOfEligibleVoters,
            int totalNumberOfVotes,
            int numberOfValidVotes,
            int numberOfNullVotes)
        {
            return new()
            {
                Division = ElectionDivision.Diaspora_Country,
                CountryId = country.Id,
                BallotId = ballot.BallotId,
                //TotalVotes = totalNumberOfVotes,
                //EligibleVoters = totalNumberOfEligibleVoters,
                ValidVotes = numberOfValidVotes,
                NullVotes = numberOfNullVotes,
            };
        }

        public void Update(int totalNumberOfEligibleVoters,
            int totalNumberOfVotes,
            int numberOfValidVotes,
            int numberOfNullVotes)
        {
            //TotalVotes = totalNumberOfVotes;
            //EligibleVoters = totalNumberOfEligibleVoters;
            ValidVotes = numberOfValidVotes;
            NullVotes = numberOfNullVotes;
        }

        public static Turnout New(int ballotBallotId, int totalNumberOfVotes, int totalNumberOfEligibleVoters, int validVotes, int nullVotes)
        {
            return new()
            {
                BallotId = ballotBallotId,
                //TotalVotes = totalNumberOfVotes,
                //EligibleVoters = totalNumberOfEligibleVoters,
                ValidVotes = validVotes,
                NullVotes = nullVotes
            };
        }

        public static Turnout CreateForDiaspora(Ballot ballot,
            int totalNumberOfEligibleVoters,
            int totalNumberOfVotes,
            int numberOfValidVotes,
            int numberOfNullVotes)
        {
            return new()
            {
                Division = ElectionDivision.Diaspora,
                BallotId = ballot.BallotId,
                //TotalVotes = totalNumberOfVotes,
                //EligibleVoters = totalNumberOfEligibleVoters,
                ValidVotes = numberOfValidVotes,
                NullVotes = numberOfNullVotes,
            };
        }

        public static Turnout CreateForUat(Ballot ballot,
            County county,
            Locality locality,
            int totalNumberOfEligibleVoters,
            int totalNumberOfVotes,
            int numberOfValidVotes,
            int numberOfNullVotes)
        {
            return new()
            {
                Division = ElectionDivision.Locality,
                CountyId = county.CountyId,
                LocalityId = locality.LocalityId,
                BallotId = ballot.BallotId,
                //TotalVotes = totalNumberOfVotes,
                //EligibleVoters = totalNumberOfEligibleVoters,
                ValidVotes = numberOfValidVotes,
                NullVotes = numberOfNullVotes,
            };
        }

        public static Turnout CreateForCounty(Ballot ballot,
            County county,
            //int totalNumberOfEligibleVoters,
            //int totalNumberOfVotes,
            int numberOfValidVotes,
            int numberOfNullVotes)
        {
            return new()
            {
                Division = ElectionDivision.County,
                CountyId = county.CountyId,
                BallotId = ballot.BallotId,
                //TotalVotes = totalNumberOfVotes,
                //EligibleVoters = totalNumberOfEligibleVoters,
                ValidVotes = numberOfValidVotes,
                NullVotes = numberOfNullVotes,
            };
        }

        public static Turnout CreateForRomania(Ballot ballot,
            int totalNumberOfEligibleVoters,
            int totalNumberOfVotes,
            int numberOfValidVotes,
            int numberOfNullVotes)
        {
            return new()
            {
                Division = ElectionDivision.National,
                BallotId = ballot.BallotId,
                //TotalVotes = totalNumberOfVotes,
                //EligibleVoters = totalNumberOfEligibleVoters,
                ValidVotes = numberOfValidVotes,
                NullVotes = numberOfNullVotes,
            };
        }
    }
}
