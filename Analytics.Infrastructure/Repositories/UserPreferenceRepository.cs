using Analytics.Domain.Entities;
using Analytics.Domain.Repositories;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class UserPreferenceRepository(AnalyticsDbContext context)
    : EfRepository<UserPreference, Guid>(context), IUserPreferenceRepository
{
    public async Task<IReadOnlyList<UserPreference>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => await context.UserPreferences
            .Where(p => p.UserId == userId)
            .ToListAsync(ct);

    public Task<UserPreference?> GetByUserAndKeyAsync(Guid userId, string preferenceKey, CancellationToken ct = default)
        => context.UserPreferences.FirstOrDefaultAsync(p => p.UserId == userId && p.PreferenceKey == preferenceKey, ct);
}
