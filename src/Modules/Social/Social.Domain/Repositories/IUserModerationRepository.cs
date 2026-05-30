using Social.Domain.Entities;

namespace Social.Domain.Repositories;

public interface IUserModerationRepository
{
    Task AddAsync(UserModerationRecord record, CancellationToken ct = default);
    Task<IReadOnlyList<UserModerationRecord>> GetByUserAsync(Guid userId, CancellationToken ct = default);
    Task<UserModerationRecord?> GetActiveBanAsync(Guid userId, DateTime now, CancellationToken ct = default);
}
