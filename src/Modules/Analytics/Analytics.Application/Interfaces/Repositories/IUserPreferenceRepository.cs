using Analytics.Domain.Entities;
using YallaJo.SharedKernel.Domain.Abstractions.Data;

namespace Analytics.Application.Interfaces.Repositories;

public interface IUserPreferenceRepository : IRepository<UserPreference, Guid>
{
    Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<IReadOnlyList<UserPreferredCategory>> GetPreferredCategoriesAsync(Guid userId, CancellationToken ct = default);
    Task UpsertPreferredCategoryAsync(UserPreferredCategory category, CancellationToken ct = default);
}
