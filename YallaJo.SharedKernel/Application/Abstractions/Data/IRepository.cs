using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YallaJo.SharedKernel.Domain.Entities;

namespace YallaJo.SharedKernel.Application.Abstractions.Data
{
    public interface IRepository<TEntity, in TKey> : IReadRepository<TEntity, TKey>, IWriteRepository<TEntity, TKey>
      where TEntity : class, IAggregateRoot
      where TKey : notnull;

    public interface IRepository<TEntity> : IRepository<TEntity, Guid>
        where TEntity : class, IAggregateRoot;
}
