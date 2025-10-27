using Microsoft.EntityFrameworkCore;

namespace Packages.Entity.Infraestructure.Factories
{
    public interface IDatabaseContextFactory
    {
        public DbContext CreateDbContext<TEntity>() where TEntity : class;
    }
}
