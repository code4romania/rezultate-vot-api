using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public interface IBallotsRepository
    {
        Task<IEnumerable<Ballot>> GetAllBallots(bool includeElection = false);
        Task<Ballot> GetBallotById(int ballotId, bool includeElection = false);
    }
}