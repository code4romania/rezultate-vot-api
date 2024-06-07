using ElectionResults.Core.Entities;

namespace ElectionResults.Hangfire.Repository;

public interface IRepository<T> : IRepositoryBase<T> where T : class, IAmEntity;
