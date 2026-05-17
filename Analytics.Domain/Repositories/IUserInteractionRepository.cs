using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Domain.Repositories;

public interface IUserInteractionRepository : IRepository<UserInteraction, long>
{
    Task<IReadOnlyList<UserInteraction>> GetByUserIdAsync(Guid userId, int take, CancellationToken ct = default);
    Task<IReadOnlyList<UserInteraction>> GetByEntityAsync(string entityType, Guid entityId, CancellationToken ct = default);
}
