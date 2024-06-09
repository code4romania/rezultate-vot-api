using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace ElectionResults.Core.Repositories
{
    public class PartiesRepository : IPartiesRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public PartiesRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<IEnumerable<Party>> GetAllParties()
        {
            return await _dbContext.Parties.ToListAsync();
        }
    }
}
