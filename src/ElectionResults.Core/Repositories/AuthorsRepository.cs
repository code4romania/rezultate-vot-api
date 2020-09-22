using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ElectionResults.Core.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace ElectionResults.Core.Repositories
{
    public class AuthorsRepository : IAuthorsRepository
    {
        private readonly ApplicationDbContext _dbContext;

        public AuthorsRepository(ApplicationDbContext dbContext)
        {
            _dbContext = dbContext;
        }

        public async Task<List<Author>> GetAuthors()
        {
            return await _dbContext.Authors
                .ToListAsync();
        }
    }
}
