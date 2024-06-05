using ElectionResults.Core.Entities;

namespace ElectionResults.Hangfire.Repository;

public interface IReadRepository<T> : IReadRepositoryBase<T> where T : class, IAmEntity;
