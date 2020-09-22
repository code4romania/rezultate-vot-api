using System.Collections.Generic;
using System.Threading.Tasks;
using CSharpFunctionalExtensions;

namespace ElectionResults.Core.Repositories
{
    public interface IElectionRepository
    {
        Task<Result<List<ElectionBallot>>> GetElections();
    }
}