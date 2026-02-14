using Microsoft.EntityFrameworkCore.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using System.Threading.Tasks;

namespace YallaJo.SharedKernel.Application.Abstractions.Data
{
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
        Task<int> ExecuteUpdateAsync(
            Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
            Expression<Func<TEntity, bool>>? filter = null,
            CancellationToken ct = default);
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
