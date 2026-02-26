using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using System.Linq.Expressions;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace YallaJo.SharedKernel.Infrastructure.Data.Repositories;

/// <summary>
/// Base repository for <b>non-aggregate-root</b> entities (Role, RoleClaim, UserClaim, …).
///
/// Design:
///   • Extends <see cref="EfReadRepository{TEntity,TKey}"/> — all read methods are inherited
///     and available directly on subclasses (no <c>_read.</c> prefix needed).
///   • Delegates write operations to <see cref="EfWriteRepository{TEntity,TKey}"/> via
///     the protected <see cref="_write"/> field.
///   • <c>protected readonly DbContext _context</c> is inherited from EfReadRepository and
///     can be cast to a typed DbContext in subclasses that need typed DbSet access.
///
/// Use <see cref="EfRepository{TEntity,TKey}"/> (requires <c>IAggregateRoot</c>) for true
/// aggregate roots such as User.
/// </summary>
public class EfEntityRepository<TEntity, TKey>
    : EfReadRepository<TEntity, TKey>, IWriteRepository<TEntity, TKey>
    where TEntity : class
    where TKey : notnull
{
    protected readonly EfWriteRepository<TEntity, TKey> _write;

    public EfEntityRepository(DbContext context) : base(context)
        => _write = new EfWriteRepository<TEntity, TKey>(context);

    // ── Write operations (delegated to EfWriteRepository) ────────────────────

    public virtual void Add(TEntity entity) => _write.Add(entity);
    public virtual void AddRange(IEnumerable<TEntity> entities) => _write.AddRange(entities);
    public virtual Task AddAsync(TEntity entity, CancellationToken ct = default) => _write.AddAsync(entity, ct);
    public virtual Task AddRangeAsync(IEnumerable<TEntity> entities, CancellationToken ct = default) => _write.AddRangeAsync(entities, ct);
    public virtual void Update(TEntity entity) => _write.Update(entity);
    public virtual void UpdateRange(IEnumerable<TEntity> entities) => _write.UpdateRange(entities);
    public virtual void Remove(TEntity entity) => _write.Remove(entity);
    public virtual void RemoveRange(IEnumerable<TEntity> entities) => _write.RemoveRange(entities);

    public virtual void AttachAndMarkModified(TEntity entity,
        params Expression<Func<TEntity, object>>[] modifiedProperties)
        => _write.AttachAndMarkModified(entity, modifiedProperties);

    public virtual void AttachAndMarkModifiedWithConcurrency(TEntity entity,
        string concurrencyPropertyName, object originalConcurrencyValue,
        params Expression<Func<TEntity, object>>[] modifiedProperties)
        => _write.AttachAndMarkModifiedWithConcurrency(entity, concurrencyPropertyName,
            originalConcurrencyValue, modifiedProperties);

    public virtual Task<int> ExecuteDeleteAsync(Expression<Func<TEntity, bool>> filter,
        CancellationToken ct = default)
        => _write.ExecuteDeleteAsync(filter, ct);

    public virtual Task<bool> DeleteByIdAsync(TKey id, CancellationToken ct = default)
        => _write.DeleteByIdAsync(id, ct);

    public virtual Task<int> ExecuteDeleteByIdsAsync(IEnumerable<TKey> ids,
        CancellationToken ct = default)
        => _write.ExecuteDeleteByIdsAsync(ids, ct);

    /// <summary>
    /// EF-specific bulk update. Not part of <see cref="IWriteRepository{TEntity,TKey}"/>
    /// by design — keeps domain interfaces free of EF coupling.
    /// </summary>
    public virtual Task<int> ExecuteUpdateAsync(
        Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
        Expression<Func<TEntity, bool>>? filter = null,
        CancellationToken ct = default)
        => _write.ExecuteUpdateAsync(setPropertyCalls, filter, ct);
}
