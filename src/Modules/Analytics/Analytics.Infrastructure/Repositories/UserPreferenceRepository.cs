using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Analytics.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using YallaJo.SharedKernel.Infrastructure.Data.Repositories;

namespace Analytics.Infrastructure.Repositories;

internal sealed class UserPreferenceRepository(AnalyticsDbContext context) : EfRepository<UserPreference, Guid>(context), IUserPreferenceRepository
{
    public Task<UserPreference?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
        => context.UserPreferences.FirstOrDefaultAsync(x => x.UserId == userId, ct);

    public async Task<IReadOnlyList<UserPreferredCategory>> GetPreferredCategoriesAsync(Guid userId, CancellationToken ct = default)
        => await context.UserPreferredCategories
            .AsNoTracking()
            .Where(x => x.UserId == userId)
            .OrderByDescending(x => x.PreferenceScore)
            .ToListAsync(ct);

    public async Task UpsertPreferredCategoryAsync(UserPreferredCategory category, CancellationToken ct = default)
    {
        var existing = await context.UserPreferredCategories.FirstOrDefaultAsync(x => x.UserId == category.UserId && x.CategoryId == category.CategoryId, ct);
        if (existing is null)
        {
            await context.UserPreferredCategories.AddAsync(category, ct);
            return;
        }

        existing.UpdateScore(category.PreferenceScore);
    }
}
