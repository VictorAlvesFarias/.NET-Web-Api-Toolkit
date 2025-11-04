using Microsoft.EntityFrameworkCore;
using Web.Api.Toolkit.Entity.Infraestructure.Factories;
using Web.Api.Toolkit.Entity.Infraestructure.Mediators;

namespace Web.Api.Toolkit.Entity.Infraestructure.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        private readonly DbSet<TEntity> _entity;
        private readonly DbContext _context;
        private readonly IDatabaseContextMediator<TEntity> _mediator;
        private readonly IDatabaseContextFactory _dbContextFactory;

        public BaseRepository(IDatabaseContextFactory dbContextFactory, IDatabaseContextMediator<TEntity> mediator)
        {
            _mediator = mediator;
            _dbContextFactory = dbContextFactory;
            _context = _dbContextFactory.CreateDbContext<TEntity>();
            _entity = _context.Set<TEntity>();
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            _mediator.Handle(entity, _context);

            var result = await _entity.AddAsync(entity);

            await _context.SaveChangesAsync();

            return result.Entity;
        }

        public bool Remove(TEntity item)
        {
            _mediator.Handle(item, _context);

            _context.Remove(item);
            _context.SaveChanges();

            return true;
        }

        public bool Remove(int id)
        {
            var entity = _entity.Find(id);

            _mediator.Handle(entity, _context);

            if (entity != null)
            {
                _context.Remove(entity);
                _context.SaveChanges();

                return true;
            }

            return false;
        }

        public bool Update(TEntity entity)
        {
            _mediator.Handle(entity, _context);

            _entity.Update(entity);
            _context.SaveChanges();

            return true;
        }

        public TEntity GetById(int id)
        {
            return _entity.Find(id);
        }

        public IQueryable<TEntity> Get()
        {
            return Get(false);
        }

        public IQueryable<TEntity> Get(bool ignoreUserId)
        {
            var query = _entity.AsQueryable();

            query = _mediator.Handle(query, _context, ignoreUserId);

            return query;
        }
    }
}
