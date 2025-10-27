namespace Entity.Infraestructure.Repositories
{
    public interface IBaseRepository<TEntity> where TEntity : class
    {
        Task<TEntity> AddAsync(TEntity entity);
        bool Remove(TEntity item);
        bool Remove(int id);
        bool Update(TEntity entity);
        TEntity GetById(int id);
        IQueryable<TEntity> Get();
        IQueryable<TEntity> Get(bool ignoreUserId);
    }
}
