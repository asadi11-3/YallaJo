using Analytics.Application.Interfaces.Repositories;
using Analytics.Contracts.Services;

namespace Analytics.Infrastructure.Services;

public sealed class UserPreferenceLookupService(IUserPreferenceRepository repository) : IUserPreferenceLookupService
{
    public async Task<UserPreferenceSummary?> GetPreferenceSummaryAsync(Guid userId, CancellationToken ct = default)
    {
        var preference = await repository.GetByUserIdAsync(userId, ct).ConfigureAwait(false);
        if (preference is null) return null;

        var categories = await repository.GetPreferredCategoriesAsync(userId, ct).ConfigureAwait(false);
        var preferredIds = categories
            .Where(c => c.PreferenceScore > 0m)
            .OrderByDescending(c => c.PreferenceScore)
            .Select(c => c.CategoryId)
            .ToList();
        var categoryScores = categories.ToDictionary(c => c.CategoryId, c => c.PreferenceScore);

        return new UserPreferenceSummary(
            preference.BudgetTier,
            preference.IsFamilyTraveler,
            preferredIds,
            categoryScores);
    }
}
