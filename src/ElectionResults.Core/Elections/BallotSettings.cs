using System.Collections.Generic;
using System.Linq;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Elections
{
    public static class BallotSettings
    {
        public static readonly Dictionary<BallotType, List<ElectionDivision>> BallotTypeMatchList;

        static BallotSettings()
        {
            BallotTypeMatchList = new Dictionary<BallotType, List<ElectionDivision>>();
            BallotTypeMatchList[BallotType.Mayor] = new List<ElectionDivision> { ElectionDivision.Locality };
            BallotTypeMatchList[BallotType.LocalCouncil] = new List<ElectionDivision> { ElectionDivision.Locality };
            BallotTypeMatchList[BallotType.CountyCouncil] = new List<ElectionDivision> { ElectionDivision.Locality, ElectionDivision.County };
            BallotTypeMatchList[BallotType.CountyCouncilPresident] = new List<ElectionDivision> { ElectionDivision.Locality, ElectionDivision.County };
            BallotTypeMatchList[BallotType.CapitalCityMayor] = new List<ElectionDivision> { ElectionDivision.County };
            BallotTypeMatchList[BallotType.CapitalCityCouncil] = new List<ElectionDivision> { ElectionDivision.County };
            BallotTypeMatchList[BallotType.President] = new List<ElectionDivision> { ElectionDivision.Locality, ElectionDivision.County, ElectionDivision.Diaspora_Country, ElectionDivision.Diaspora, ElectionDivision.National };
            BallotTypeMatchList[BallotType.EuropeanParliament] = new List<ElectionDivision> { ElectionDivision.Locality, ElectionDivision.County, ElectionDivision.Diaspora_Country, ElectionDivision.Diaspora, ElectionDivision.National };
            BallotTypeMatchList[BallotType.Senate] = new List<ElectionDivision> { ElectionDivision.Locality, ElectionDivision.County, ElectionDivision.Diaspora_Country, ElectionDivision.Diaspora, ElectionDivision.National };
            BallotTypeMatchList[BallotType.House] = new List<ElectionDivision> { ElectionDivision.Locality, ElectionDivision.County, ElectionDivision.Diaspora_Country, ElectionDivision.Diaspora, ElectionDivision.National };
            BallotTypeMatchList[BallotType.Referendum] = new List<ElectionDivision> { ElectionDivision.Locality, ElectionDivision.County, ElectionDivision.Diaspora_Country, ElectionDivision.Diaspora, ElectionDivision.National };
        }
    }
}
