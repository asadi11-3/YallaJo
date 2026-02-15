using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Application.Abstractions.Data;
using YallaJo.SharedKernel.Application.Abstractions.Pagination;
using YallaJo.SharedKernel.Application.Abstractions.Specifications;
using YallaJo.SharedKernel.Infrastructure.Specifications;

namespace YallaJo.SharedKernel.Infrastructure.Data.Repositories
{
    public class EfReadRepository<TEntity, TKey> : IReadRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
    {
        protected readonly DbContext _context;
        protected readonly DbSet<TEntity> _dbSet;
        

       

        protected DbSet<TEntity> DbSet => _context.Set<TEntity>();

        public EfReadRepository(DbContext context)
        {
            _context = context;
            _dbSet = context.Set<TEntity>();
        }

        public virtual async Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default, bool asNoTracking = true)
            => await _dbSet.FindAsync([id], ct);

                .FirstOrDefaultAsync(e => EF.Property<TKey>(e, "Id")!.Equals(id), ct);
        public virtual async Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            bool asNoTracking = true, CancellationToken ct = default)
            => await BuildQuery(filter, include, asNoTracking: asNoTracking).FirstOrDefaultAsync(ct);

        public virtual async Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool asNoTracking = true, CancellationToken ct = default)
            => await BuildQuery(filter, include, orderBy, asNoTracking).FirstOrDefaultAsync(ct);

        public virtual async Task<TEntity?> SingleOrDefaultAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            bool asNoTracking = true, CancellationToken ct = default)
            => await BuildQuery(filter, include, asNoTracking: asNoTracking).SingleOrDefaultAsync(ct);

        public virtual async Task<List<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool asNoTracking = true, CancellationToken ct = default)
            => await BuildQuery(filter, include, orderBy, asNoTracking).ToListAsync(ct);

        public virtual async Task<PaginatedResult<TEntity>> GetPaginatedAsync(
            int pageNumber, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool asNoTracking = true, CancellationToken ct = default)
        {
            var query = BuildQuery(filter, include, orderBy, asNoTracking);
            var totalCount = await query.CountAsync(ct);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToListAsync(ct);
            return new PaginatedResult<TEntity>(items, totalCount, pageNumber, pageSize);
        }

        public virtual async Task<List<TResult>> SelectAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            if (filter is not null) query = query.Where(filter);
            if (orderBy is not null) query = orderBy(query);
            return await query.Select(selector).ToListAsync(ct);
        }

        public virtual async Task<PaginatedResult<TResult>> SelectPaginatedAsync<TResult>(
            int pageNumber, int pageSize,
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken ct = default)
        {
            IQueryable<TEntity> query = _dbSet.AsNoTracking();
            if (filter is not null) query = query.Where(filter);
            var totalCount = await query.CountAsync(ct);
            if (orderBy is not null) query = orderBy(query);
            var items = await query.Skip((pageNumber - 1) * pageSize).Take(pageSize).Select(selector).ToListAsync(ct);
            return new PaginatedResult<TResult>(items, totalCount, pageNumber, pageSize);
        }

        public virtual IQueryable<TEntity> Query(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            bool asNoTracking = true)
            => BuildQuery(filter, include, asNoTracking: asNoTracking);

        public virtual async Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default)
            => await _dbSet.AnyAsync(predicate, ct);

        public virtual async Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct = default)
            => filter is null ? await _dbSet.AnyAsync(ct) : await _dbSet.AnyAsync(filter, ct);

        public virtual async Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct = default)
            => filter is null ? await _dbSet.CountAsync(ct) : await _dbSet.CountAsync(filter, ct);

        protected IQueryable<TEntity> BuildQuery(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool asNoTracking = true)
        {
            IQueryable<TEntity> query = _dbSet;
            if (asNoTracking) query = query.AsNoTracking();
            if (include is not null) query = include(query);
            if (filter is not null) query = query.Where(filter);
            if (orderBy is not null) query = orderBy(query);
            return query;
        }

        public async Task<TEntity?> FirstOrDefaultAsync(
          ISpecification<TEntity> specification,
          CancellationToken ct = default)
        {
            return await ApplySpecification(specification).FirstOrDefaultAsync(ct);
        }

        public async Task<TEntity?> SingleOrDefaultAsync(
            ISpecification<TEntity> specification,
            CancellationToken ct = default)
        {
            return await ApplySpecification(specification).SingleOrDefaultAsync(ct);
        }

        // ─── Collections ─────────────────────────────────────────────────

        public async Task<List<TEntity>> ListAsync(
            ISpecification<TEntity> specification,
            CancellationToken ct = default)
        {
            return await ApplySpecification(specification).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TEntity>> ReadOnlyListAsync(
            ISpecification<TEntity> specification,
            CancellationToken ct = default)
        {
            return await ApplySpecification(specification).ToListAsync(ct);
        }

        // ─── Projected Collections ───────────────────────────────────────

        public async Task<List<TResult>> ListAsync<TResult>(
            ISpecification<TEntity, TResult> specification,
            CancellationToken ct = default)
        {
            return await ApplySpecification(specification).ToListAsync(ct);
        }

        public async Task<IReadOnlyList<TResult>> ReadOnlyListAsync<TResult>(
            ISpecification<TEntity, TResult> specification,
            CancellationToken ct = default)
        {
            return await ApplySpecification(specification).ToListAsync(ct);
        }

        // ─── Aggregates ──────────────────────────────────────────────────

        public async Task<int> CountAsync(
            ISpecification<TEntity> specification,
            CancellationToken ct = default)
        {
            return await ApplySpecification(specification, evaluatePaging: false).CountAsync(ct);
        }

        public async Task<bool> AnyAsync(
            ISpecification<TEntity> specification,
            CancellationToken ct = default)
        {
            return await ApplySpecification(specification, evaluatePaging: false).AnyAsync(ct);
        }

        // ─── Paged Results ───────────────────────────────────────────────

        public async Task<PaginatedResult<TEntity>> PaginatedListAsync(
     ISpecification<TEntity> specification,
     CancellationToken ct = default)
        {
            var totalCount = await ApplySpecification(specification, evaluatePaging: false).CountAsync(ct);

            var items = await ApplySpecification(specification).ToListAsync(ct);

            var pageSize = specification.Take ?? items.Count;
            var pageNumber = (specification.Skip.HasValue && pageSize > 0)
                ? (specification.Skip.Value / pageSize) + 1
                : 1;

            return new PaginatedResult<TEntity>(items, totalCount, pageNumber, pageSize);
        }


        public async Task<PaginatedResult<TResult>> PaginatedListAsync<TResult>(
     ISpecification<TEntity, TResult> specification,
     CancellationToken ct = default)
        {
            var totalCount = await ApplySpecification(specification, evaluatePaging: false).CountAsync(ct);

            var items = await ApplySpecification(specification).ToListAsync(ct);

            var pageSize = specification.Take ?? items.Count;
            var pageNumber = (specification.Skip.HasValue && pageSize > 0)
                ? (specification.Skip.Value / pageSize) + 1
                : 1;

            return new PaginatedResult<TResult>(items, totalCount, pageNumber, pageSize);
        }



        private IQueryable<TEntity> ApplySpecification(
     ISpecification<TEntity> specification,
     bool evaluatePaging = true)
        {
            var spec = (!evaluatePaging && specification.IsPagingEnabled)
                ? new NoPagingSpecWrapper<TEntity>(specification)
                : specification;

            return SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), spec);
        }



        private IQueryable<TResult> ApplySpecification<TResult>(
            ISpecification<TEntity, TResult> specification,
            bool evaluatePaging = true)
        {
            if (!evaluatePaging && specification.IsPagingEnabled)
            {
                var baseSpec = new NoPagingSpecWrapper<TEntity>(specification);
                var baseQuery = SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), baseSpec);

                if (specification.SelectorExpression is not null)
                    return baseQuery.Select(specification.SelectorExpression);

                throw new InvalidOperationException("SelectorExpression is required for typed projections.");
            }

            return SpecificationEvaluator.GetQuery(DbSet.AsQueryable(), specification);
        }
    }
}
