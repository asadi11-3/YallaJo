using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Domain.Abstractions.Data;
using YallaJo.SharedKernel.Domain.Abstractions.Pagination;
using YallaJo.SharedKernel.Domain.Abstractions.Specifications;
using YallaJo.SharedKernel.Domain.Entities;

namespace YallaJo.SharedKernel.Infrastructure.Data.Repositories
{
    public class EfRepository<TEntity, TKey> : IRepository<TEntity, TKey>
     where TEntity : class, IAggregateRoot
     where TKey : notnull
    {
        private readonly EfReadRepository<TEntity, TKey> _read;
        private readonly EfWriteRepository<TEntity, TKey> _write;

        public EfRepository(DbContext context)
        {
            _read = new EfReadRepository<TEntity, TKey>(context);
            _write = new EfWriteRepository<TEntity, TKey>(context);
        }

        #region Read Methods
        public Task<TEntity?> GetByIdAsync(TKey id, CancellationToken ct = default, bool asNoTracking = true)
            => _read.GetByIdAsync(id, ct, asNoTracking);
        public Task<TEntity?> GetAsync(Expression<Func<TEntity, bool>> filter, Func<IQueryable<TEntity>,
            IQueryable<TEntity>>? include = null, bool asNoTracking = true, CancellationToken ct = default)
            => _read.GetAsync(filter, include, asNoTracking, ct);
        public Task<TEntity?> FirstOrDefaultAsync(Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, Func<IQueryable<TEntity>,
             IOrderedQueryable<TEntity>>? orderBy = null, bool asNoTracking = true, CancellationToken ct = default) =>
            _read.FirstOrDefaultAsync(filter, include, orderBy, asNoTracking, ct);
        public Task<TEntity?> SingleOrDefaultAsync(Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, bool asNoTracking = true,
            CancellationToken ct = default) => _read.SingleOrDefaultAsync(filter, include, asNoTracking, ct);
        public Task<List<TEntity>> GetAllAsync(Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, Func<IQueryable<TEntity>,
                IOrderedQueryable<TEntity>>? orderBy = null, bool asNoTracking = true, CancellationToken ct = default) =>
            _read.GetAllAsync(filter, include, orderBy, asNoTracking, ct);
        public Task<PaginatedResult<TEntity>> GetPaginatedAsync(int pageNumber, int pageSize,
            Expression<Func<TEntity, bool>>? filter = null, Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, bool asNoTracking = true,
            CancellationToken ct = default) => _read.GetPaginatedAsync(pageNumber, pageSize, filter, include, orderBy, asNoTracking, ct);
        public Task<List<TResult>> SelectAsync<TResult>(Expression<Func<TEntity, TResult>> selector,
            Expression<Func<TEntity, bool>>? filter = null, Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null,
            CancellationToken ct = default) => _read.SelectAsync(selector, filter, orderBy, ct);
        public Task<PaginatedResult<TResult>> SelectPaginatedAsync<TResult>(int pageNumber, int pageSize,
            Expression<Func<TEntity, TResult>> selector, Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IOrderedQueryable<TEntity>>? orderBy = null, CancellationToken ct = default) =>
            _read.SelectPaginatedAsync(pageNumber, pageSize, selector, filter, orderBy, ct);
        public IQueryable<TEntity> Query(Expression<Func<TEntity, bool>>? filter = null,
            Func<IQueryable<TEntity>, IQueryable<TEntity>>? include = null, bool asNoTracking = true)
            => _read.Query(filter, include, asNoTracking);
        public Task<bool> ExistsAsync(Expression<Func<TEntity, bool>> predicate,
            CancellationToken ct = default) => _read.ExistsAsync(predicate, ct);
        public Task<bool> AnyAsync(Expression<Func<TEntity, bool>>? filter = null,
            CancellationToken ct = default) => _read.AnyAsync(filter, ct);
        public Task<int> CountAsync(Expression<Func<TEntity, bool>>? filter = null,
            CancellationToken ct = default) => _read.CountAsync(filter, ct);
        #endregion

        #region Write Methods
        public void Add(TEntity entity) => _write.Add(entity);
        public void AddRange(IEnumerable<TEntity> entities) => _write.AddRange(entities);
        public Task AddAsync(TEntity entity, CancellationToken ct = default) => _write.AddAsync(entity, ct);
        public Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) => _write.AddRangeAsync(entities, ct);
        public void Update(TEntity entity) => _write.Update(entity);
        public void UpdateRange(IEnumerable<TEntity> entities) => _write.UpdateRange(entities);
        public Task<int> ExecuteUpdateAsync(Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
            Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct = default)
            => _write.ExecuteUpdateAsync(setPropertyCalls, filter, ct);
        public void AttachAndMarkModified(TEntity entity, params Expression<Func<TEntity, object>>[] modifiedProperties)
            => _write.AttachAndMarkModified(entity, modifiedProperties);
        public void AttachAndMarkModifiedWithConcurrency(TEntity entity, string concurrencyPropertyName,
            object originalConcurrencyValue, params Expression<Func<TEntity, object>>[] modifiedProperties)
            => _write.AttachAndMarkModifiedWithConcurrency(entity, concurrencyPropertyName, originalConcurrencyValue, modifiedProperties);
        public void Remove(TEntity entity) => _write.Remove(entity);
        public void RemoveRange(IEnumerable<TEntity> entities) => _write.RemoveRange(entities);
        public Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
            => _write.ExecuteDeleteAsync(filter, ct);
        public Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default) => _write.DeleteByIdAsync(id, ct);
        public Task<int> ExecuteDeleteByIdsAsync(IEnumerable<TKey> ids, CancellationToken ct = default)
            => _write.ExecuteDeleteByIdsAsync(ids, ct);
        #endregion

        #region Specification Methods
        public Task<TEntity?> FirstOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
           => _read.FirstOrDefaultAsync(specification, ct);
        public Task<TEntity?> SingleOrDefaultAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
           => _read.SingleOrDefaultAsync(specification, ct);
        public Task<List<TEntity>> ListAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
           => _read.ListAsync(specification, ct);
        public Task<IReadOnlyList<TEntity>> ReadOnlyListAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
           => _read.ReadOnlyListAsync(specification, ct);
        public Task<List<TResult>> ListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken ct = default)
           => _read.ListAsync(specification, ct);
        public Task<IReadOnlyList<TResult>> ReadOnlyListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken ct = default)
           => _read.ReadOnlyListAsync(specification, ct);
        public Task<int> CountAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
            => _read.CountAsync(specification, ct);
        public Task<bool> AnyAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
           => _read.AnyAsync(specification, ct);
        public Task<PaginatedResult<TEntity>> PaginatedListAsync(ISpecification<TEntity> specification, CancellationToken ct = default)
           => _read.PaginatedListAsync(specification, ct);
        public Task<PaginatedResult<TResult>> PaginatedListAsync<TResult>(ISpecification<TEntity, TResult> specification, CancellationToken ct = default)
            => _read.PaginatedListAsync(specification, ct);
        #endregion
    }

    public class EfRepository<TEntity> : EfRepository<TEntity, Guid>, IRepository<TEntity>
        where TEntity : class, IAggregateRoot
    {
        public EfRepository(DbContext context) : base(context) { }
    }
}
