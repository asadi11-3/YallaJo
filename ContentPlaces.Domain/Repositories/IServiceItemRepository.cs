using ContentPlaces.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace ContentPlaces.Domain.Repositories;

/// <summary>
/// ServiceItem is a non-aggregate entity (AuditableEntity, not IAggregateRoot).
/// Uses IReadRepository + IWriteRepository rather than IRepository which requires IAggregateRoot.
/// </summary>
public interface IServiceItemRepository : IReadRepository<ServiceItem, Guid>, IWriteRepository<ServiceItem, Guid>
{
}
