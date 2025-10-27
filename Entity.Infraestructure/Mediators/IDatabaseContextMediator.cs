using Microsoft.EntityFrameworkCore;

namespace Packages.Entity.Infraestructure.Mediators
{
    public interface IDatabaseContextMediator<TEntity> where TEntity: class
    {
        IQueryable<TEntity> Handle(IQueryable<TEntity> query, DbContext context, bool ignoreUserId);
        void Handle(TEntity entity, DbContext context);
    }
}
