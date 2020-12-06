using System.Threading.Tasks;
using CSharpFunctionalExtensions;
using ElectionResults.Core.Endpoints.Query;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Scheduler
{
    public interface IResultsCrawler
    {
        Task<Result<LiveElectionInfo>> Import(string url);
        Task ImportAllResults();
        Task<LiveElectionInfo> AggregateNationalResults(ElectionResultsQuery query, Ballot ballot);
        Task<LiveElectionInfo> AggregateDiasporaResults(ElectionResultsQuery query, Ballot ballot);
        Task<LiveElectionInfo> ImportCapitalCityResults(Ballot ballot);
    }
}