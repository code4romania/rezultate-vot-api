using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;

namespace ElectionResults.Core.Repositories
{
    public interface IPartiesRepository
    {
        Task<List<Party>> GetAllParties();
    }
}