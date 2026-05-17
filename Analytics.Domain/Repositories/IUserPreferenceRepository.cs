using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Domain.Repositories;

public interface IUserPreferenceRepository : IRepository<UserPreference, Guid>
{
    Task<IReadOnlyList<UserPreference>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<UserPreference?> GetByUserAndKeyAsync(Guid userId, string preferenceKey, CancellationToken ct = default);
}
