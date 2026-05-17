using Social.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Social.Domain.Repositories;

public interface IFavoriteRepository : IRepository<Favorite, Guid>
{
    Task<IReadOnlyList<Favorite>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<int> CountByUserAsync(Guid userId, CancellationToken ct = default);
    Task<Favorite?> GetByUserAndEntityAsync(Guid userId, string entityType, Guid entityId, CancellationToken ct = default);
}
