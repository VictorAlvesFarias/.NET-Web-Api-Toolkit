namespace Web.Api.Toolkit.Entity.Infraestructure.Repositories
{
    public interface IBaseRepository<TEntity> : IBaseRepositoryOperations<TEntity> where TEntity : class
    {
        BaseRepositoryWrapper<TEntity> IgnoreMediator<TMediator>();
        BaseRepositoryWrapper<TEntity> IgnoreMediator(Type mediatorType);
        Task<TEntity> AddAsync(TEntity entity, IReadOnlyCollection<Type> ignoreMediators);
        bool Remove(TEntity item, IReadOnlyCollection<Type> ignoreMediators);
        bool Remove(int id, IReadOnlyCollection<Type> ignoreMediators);
        bool Update(TEntity entity, IReadOnlyCollection<Type> ignoreMediators);
        IQueryable<TEntity> Get(IReadOnlyCollection<Type> ignoreMediators);
    }


    public interface IBaseRepositoryOperations<TEntity> where TEntity : class
    {
        Task<TEntity> AddAsync(TEntity entity);
        bool Remove(TEntity item);
        bool Remove(int id);
        bool Update(TEntity entity);
        IQueryable<TEntity> Get();
    }
}
