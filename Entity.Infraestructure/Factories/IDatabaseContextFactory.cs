using Microsoft.EntityFrameworkCore;

namespace Web.Api.Toolkit.Web.Api.Toolkit.Entity.Infraestructure.Factories
{
    public interface IDatabaseContextFactory
    {
        public DbContext CreateDbContext<TEntity>() where TEntity : class;
    }
}
