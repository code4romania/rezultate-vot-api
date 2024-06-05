using ElectionResults.Core.Entities;
using ElectionResults.Core.Repositories;

namespace ElectionResults.Hangfire.Repository;

public class EfRepository<T> : RepositoryBase<T>, IReadRepository<T>, IRepository<T> where T : class, IAmEntity
{
    public EfRepository(ApplicationDbContext dbContext) : base(dbContext)
    {
    }
}