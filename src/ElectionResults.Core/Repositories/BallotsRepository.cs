using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class BallotsRepository : IBallotsRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public BallotsRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Ballot>> GetAllBallots(bool includeElection = false)
        {
            return await  CreateQueryable(includeElection).ToListAsync();
        }

        public async Task<Ballot> GetBallotById(int ballotId, bool includeElection = false)
        {

            var query = CreateQueryable(includeElection);
            var ballot = await query.FirstOrDefaultAsync(b => b.BallotId == ballotId);

            return ballot;
        }

        private IQueryable<Ballot> CreateQueryable(bool includeElection)
        {
            var query = _dbContext.Ballots.AsNoTracking();
            if (includeElection)
                query = query.Include(b => b.Election);
            return query;
        }
    }
}