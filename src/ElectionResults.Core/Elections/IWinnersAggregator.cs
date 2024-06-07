using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Response;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Elections;

public interface IWinnersAggregator
{
    Task<Result<List<ElectionMapWinner>>> GetCountryWinners(int ballotId);

    Task<Result<List<ElectionMapWinner>>> GetCountyWinners(int ballotId);

    Task<Result<List<ElectionMapWinner>>> GetLocalityWinnersByCounty(int ballotId, int countyId);

    Task<Result<List<Winner>>> GetLocalityCityHallWinnersByCounty(int ballotId, int countyId, bool takeOnlyWinner = true);

    List<CandidateResult> RetrieveWinners(List<CandidateResult> results,
        BallotType ballotType);

    Task<Result<List<CandidateResult>>> GetWinningCandidatesByCounty(int ballotId);
}