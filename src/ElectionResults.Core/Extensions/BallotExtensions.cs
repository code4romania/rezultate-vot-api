using System.Linq;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Extensions
{
    public static class BallotExtensions
    {
        public static bool DoesNotAllowDivision(this Ballot ballot, ElectionDivision division)
        {
            return BallotSettings.BallotTypeMatchList[ballot.BallotType].All(t => t != division);
        }
    }
}
