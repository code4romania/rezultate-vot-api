using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace ElectionResults.Core.Entities
{
    [Table("observations")]
    public class Observation : IAmEntity
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
        public static Observation Create(Ballot ballot, int startedForms, int coveredPollingPlaces, int coveredCounties, int flaggedAnswers, int observers)
        {
            return new()
            {
                BallotId = ballot.BallotId,
                MessageCount = startedForms,
                CoveredPollingPlaces = coveredPollingPlaces,
                CoveredCounties = coveredCounties,
                IssueCount = flaggedAnswers,
                ObserverCount = observers
            };
        }

        public void Update(int startedForms, int coveredPollingPlaces, int coveredCounties, int flaggedAnswers,
            int observers)
        {
            MessageCount = startedForms;
            CoveredPollingPlaces = coveredPollingPlaces;
            CoveredCounties = coveredCounties;
            IssueCount = flaggedAnswers;
            ObserverCount = observers;
        }
    }

}