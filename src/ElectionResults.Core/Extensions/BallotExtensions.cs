using System.Linq;
using ElectionResults.Core.Elections;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Extensions
{
    public static class BallotExtensions
    {
        public static bool AllowsDivision(this Ballot ballot, ElectionDivision division, int localityId)
        {
            if (ballot.BallotType == BallotType.Mayor && division == ElectionDivision.County && localityId.IsCapitalCity())
            {
                return true;
            }
            return BallotSettings.BallotTypeMatchList[ballot.BallotType].Any(t => t == division);
        }

        public static bool IsCapitalCity(this int id)
        {
            return id == 12913;
        }

        public static bool IsDiaspora(this int id)
        {
            return id == 16820;
        }
    }
}
