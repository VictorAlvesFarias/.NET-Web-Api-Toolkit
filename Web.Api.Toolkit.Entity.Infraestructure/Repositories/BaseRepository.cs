using Microsoft.EntityFrameworkCore;
using Web.Api.Toolkit.Entity.Infraestructure.Factories;
using Web.Api.Toolkit.Entity.Infraestructure.Mediators;

namespace Web.Api.Toolkit.Entity.Infraestructure.Repositories
{
    public class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : class
    {
        private readonly DbSet<TEntity> _entity;
        private readonly DbContext _context;
        private readonly IEnumerable<IDatabaseContextMediator<TEntity>> _mediators;

        public BaseRepository(
            IDatabaseContextFactory dbContextFactory,
            IEnumerable<IDatabaseContextMediator<TEntity>> mediators
        )
        {
            _mediators = mediators ?? Enumerable.Empty<IDatabaseContextMediator<TEntity>>();
            _context = dbContextFactory.CreateDbContext<TEntity>();
            _entity = _context.Set<TEntity>();
        }

        public BaseRepositoryWrapper<TEntity> IgnoreMediator<TMediator>()
        {
            return IgnoreMediator(typeof(TMediator));
        }

        public BaseRepositoryWrapper<TEntity> IgnoreMediator(Type mediatorType)
        {
            return new BaseRepositoryWrapper<TEntity>(
                this,
                new[] { mediatorType }
            );
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            return await AddAsync(entity, Array.Empty<Type>());
        }

        public async Task<TEntity> AddAsync(TEntity entity, IReadOnlyCollection<Type> ignoredMediators)
        {
            foreach (var mediator in _mediators)
            {
                if (ShouldIgnore(mediator.GetType(), ignoredMediators))
                    continue;

                mediator.Handle(entity, _context);
            }

            var result = await _entity.AddAsync(entity);
            await _context.SaveChangesAsync();

            return result.Entity;
        }

        public bool Remove(TEntity item)
        {
            return Remove(item, Array.Empty<Type>());
        }

        public bool Remove(TEntity item, IReadOnlyCollection<Type> ignoredMediators )
        {
            foreach (var mediator in _mediators)
            {
                if (ShouldIgnore(mediator.GetType(), ignoredMediators))
                    continue;

                mediator.Handle(item, _context);
            }

            _context.Remove(item);
            _context.SaveChanges();

            return true;
        }

        public bool Remove(int id)
        {
            return Remove(id, Array.Empty<Type>());
        }

        public bool Remove(int id,  IReadOnlyCollection<Type> ignoredMediators)
        {
            var entity = _entity.Find(id);

            if (entity == null)
                return false;

            foreach (var mediator in _mediators)
            {
                if (ShouldIgnore(mediator.GetType(), ignoredMediators))
                    continue;

                mediator.Handle(entity, _context);
            }

            _context.Remove(entity);
            _context.SaveChanges();

            return true;
        }

        public bool Update(TEntity entity)
        {
            return Update(entity, Array.Empty<Type>());
        }

        public bool Update(TEntity entity, IReadOnlyCollection<Type> ignoredMediators)
        {
            foreach (var mediator in _mediators)
            {
                if (ShouldIgnore(mediator.GetType(), ignoredMediators))
                    continue;

                mediator.Handle(entity, _context);
            }

            _entity.Update(entity);
            _context.SaveChanges();

            return true;
        }

        public IQueryable<TEntity> Get()
        {
            return Get(Array.Empty<Type>());
        }

        public IQueryable<TEntity> Get(IReadOnlyCollection<Type> ignoredMediators)
        {
            var query = _entity.AsQueryable();

            foreach (var mediator in _mediators)
            {
                if (ShouldIgnore(mediator.GetType(), ignoredMediators))
                    continue;

                query = mediator.Handle(query, _context);
            }

            return query;
        }

        private static bool ShouldIgnore(Type mediatorType, IReadOnlyCollection<Type> ignoredMediators
        )
        {
            foreach (var ignored in ignoredMediators)
            {
                if (ignored.IsGenericTypeDefinition)
                {
                    if (mediatorType
                        .GetInterfaces()
                        .Any(i =>
                            i.IsGenericType &&
                            i.GetGenericTypeDefinition() == ignored
                        ))
                        return true;
                }
                else if (ignored.IsAssignableFrom(mediatorType))
                {
                    return true;
                }
            }

            return false;
        }
    }

    public class BaseRepositoryWrapper<TEntity> where TEntity : class
    {
        private readonly BaseRepository<TEntity> _repository;
        private readonly HashSet<Type> _ignoredMediators;

        public BaseRepositoryWrapper(
            BaseRepository<TEntity> repository,
            IEnumerable<Type> ignoredMediators
        )
        {
            _repository = repository;
            _ignoredMediators = ignoredMediators.ToHashSet();
        }

        public BaseRepositoryWrapper<TEntity> IgnoreMediator<TMediator>()
        {
            return IgnoreMediator(typeof(TMediator));
        }

        public BaseRepositoryWrapper<TEntity> IgnoreMediator(Type mediatorType)
        {
            _ignoredMediators.Add(mediatorType);
            return this;
        }

        public Task<TEntity> AddAsync(TEntity entity)
        {
            return _repository.AddAsync(entity, _ignoredMediators);
        }

        public bool Remove(TEntity item)
        {
            return _repository.Remove(item, _ignoredMediators);
        }

        public bool Remove(int id)
        {
            return _repository.Remove(id, _ignoredMediators);
        }

        public bool Update(TEntity entity)
        {
            return _repository.Update(entity, _ignoredMediators);
        }

        public IQueryable<TEntity> Get()
        {
            return _repository.Get(_ignoredMediators);
        }
    }
}
