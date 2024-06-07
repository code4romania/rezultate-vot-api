using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Endpoints.Response;

namespace ElectionResults.Core.Elections
{
    public interface IResultsAggregator
    {
        Task<Result<List<ElectionMeta>>> GetAllBallots();

        Task<Result<ElectionResponse>> GetBallotResults(ElectionResultsQuery query);

        Task<List<ArticleResponse>> GetNewsFeed(ElectionResultsQuery query, int electionId);

        Task<Result<List<PartyList>>> GetBallotCandidates(ElectionResultsQuery query);
    }
}