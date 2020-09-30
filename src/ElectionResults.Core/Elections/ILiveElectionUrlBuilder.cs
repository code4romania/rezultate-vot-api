using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Elections
{
    public interface ILiveElectionUrlBuilder
    {
        Result<string> GetFileUrl(BallotType ballotType, ElectionDivision division, string countyShortName, int? siruta);
    }
}