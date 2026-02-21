using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace YallaJo.SharedKernel.Infrastructure.Data.Repositories
{
    public class EfWriteRepository<TEntity, TKey>(DbContext context) : IWriteRepository<TEntity, TKey>
        where TEntity : class
        where TKey : notnull
    {
        protected readonly DbContext _context = context;
        protected DbSet<TEntity> DbSet => _context.Set<TEntity>();

        public virtual void Add(TEntity entity) => DbSet.Add(entity);
        public virtual void AddRange(IEnumerable<TEntity> entities) => DbSet.AddRange(entities);
        public virtual async Task AddAsync(TEntity entity, CancellationToken ct = default) => await DbSet.AddAsync(entity, ct);
        public virtual async Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) => await DbSet.AddRangeAsync(entities, ct);
        public virtual void Update(TEntity entity) => DbSet.Update(entity);
        public virtual void UpdateRange(IEnumerable<TEntity> entities) => DbSet.UpdateRange(entities);

        public virtual async Task<int> ExecuteUpdateAsync(
            Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
            Expression<Func<TEntity, bool>>? filter = null, CancellationToken ct = default)
        {
            var query = DbSet.AsQueryable();
            if (filter != null) query = query.Where(filter);
            return await query.ExecuteUpdateAsync(setPropertyCalls, ct);
        }

        public virtual void AttachAndMarkModified(TEntity entity, params Expression<Func<TEntity, object>>[] modifiedProperties)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached) DbSet.Attach(entity);
            foreach (var property in modifiedProperties)
                entry.Property(property).IsModified = true;
        }

        public virtual void AttachAndMarkModifiedWithConcurrency(TEntity entity, string concurrencyPropertyName,
            object originalConcurrencyValue, params Expression<Func<TEntity, object>>[] modifiedProperties)
        {
            var entry = _context.Entry(entity);
            if (entry.State == EntityState.Detached) DbSet.Attach(entity);
            entry.Property(concurrencyPropertyName).OriginalValue = originalConcurrencyValue;
            foreach (var property in modifiedProperties)
                entry.Property(property).IsModified = true;
        }

        public virtual void Remove(TEntity entity) => DbSet.Remove(entity);
        public virtual void RemoveRange(IEnumerable<TEntity> entities) => DbSet.RemoveRange(entities);

        public virtual async Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default)
            => await DbSet.Where(filter).ExecuteDeleteAsync(ct);

        public virtual async Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default)
        {
            var count = await DbSet.Where(e => EF.Property<TKey>(e, "Id")!.Equals(id)).ExecuteDeleteAsync(ct);
            return count > 0;
        }

        public virtual async Task<int> ExecuteDeleteByIdsAsync(IEnumerable<TKey> ids, CancellationToken ct = default)
            => await DbSet.Where(e => ids.Contains(EF.Property<TKey>(e, "Id"))).ExecuteDeleteAsync(ct);
    }
}
