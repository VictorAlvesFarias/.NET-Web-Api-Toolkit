using Microsoft.EntityFrameworkCore;

namespace Entity.Infraestructure.Factories
{
    public interface IDatabaseContextFactory
    {
        public DbContext CreateDbContext<TEntity>() where TEntity : class;
    }
}
