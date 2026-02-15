using System.Linq.Expressions;
using YallaJo.SharedKernel.Application.Abstractions.Pagination;
using YallaJo.SharedKernel.Application.Abstractions.Specifications;

namespace YallaJo.SharedKernel.Application.Abstractions.Data
{
    public interface IReadRepository<TEntity, in TKey>
        where TEntity : class
        where TKey : notnull
    {
        // Expression-based queries
        Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default, bool asNoTracking = true);

        Task<TEntity?> GetAsync(
            Expression<Func<TEntity, bool>> filter,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            bool asNoTracking = true,
            CancellationToken ct = default);

        Task<TEntity?> FirstOrDefaultAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool asNoTracking = true,
            CancellationToken ct = default);

        Task<TEntity?> SingleOrDefaultAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            bool asNoTracking = true,
            CancellationToken ct = default);

        Task<List<TEntity>> GetAllAsync(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool asNoTracking = true,
            CancellationToken ct = default);

        Task<PaginatedResult<TEntity>> GetPaginatedAsync(
            int pageNumber, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            bool asNoTracking = true,
            CancellationToken ct = default);

        Task<List<TResult>> SelectAsync<TResult>(
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken ct = default);

        Task<PaginatedResult<TResult>> SelectPaginatedAsync<TResult>(
            int pageNumber, int pageSize,
            Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken ct = default);

        IQueryable<TEntity> Query(
            Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            bool asNoTracking = true);

        Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate, CancellationToken ct = default);
        Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct = default);
        Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct = default);

        // Specification-based queries
        Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
        Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
        Task<List<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
        Task<IReadOnlyList<TEntity>> ReadOnlyListAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
        Task<List<TResult>> ListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken ct = default);
        Task<IReadOnlyList<TResult>> ReadOnlyListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken ct = default);
        Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
        Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
        Task<PaginatedResult<TEntity>> PaginatedListAsync(ISpecification<TEntity> specification, CancellationToken ct = default);
        Task<PaginatedResult<TResult>> PaginatedListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken ct = default);
    }
}
