using System.Linq.Expressions;

namespace YallaJo.SharedKernel.Domain.Abstractions.Data
{
    /// <summary>
    /// Domain-level write repository interface. Contains no EF Core dependencies.
    /// The EF-specific ExecuteUpdateAsync (which uses SetPropertyCalls&lt;T&gt;) lives
    /// only on the concrete EfWriteRepository implementation.
    /// </summary>
    public interface IWriteRepository<TEntity, in TKey>
       where TEntity : class
       where TKey : notnull
    {
        void Add(TEntity entity);
        void AddRange(IEnumerable<TEntity> entities);
        Task AddAsync(TEntity entity, CancellationToken ct = default);
        Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default);

        void Update(TEntity entity);
        void UpdateRange(IEnumerable<TEntity> entities);
        void AttachAndMarkModified(TEntity entity, params Expression<Func<TEntity, object>>[] modifiedProperties);
        void AttachAndMarkModifiedWithConcurrency(TEntity entity, string concurrencyPropertyName,
            object originalConcurrencyValue, params Expression<Func<TEntity, object>>[] modifiedProperties);

        void Remove(TEntity entity);
        void RemoveRange(IEnumerable<TEntity> entities);
        Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> filter, CancellationToken ct = default);
        Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default);
        Task<int> ExecuteDeleteByIdsAsync(IEnumerable<TKey> ids, CancellationToken ct = default);
    }
}
