using Analytics.Domain.Entities;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IUserExcludedEntityRepository : IRepository<UserExcludedEntity, Guid>
{
    Task<IReadOnlyList<UserExcludedEntity>> GetActiveByUserAsync(Guid userId, CancellationToken ct = default);
    Task<bool> IsExcludedAsync(Guid userId, EntityType entityKind, Guid entityId, CancellationToken ct = default);
}
