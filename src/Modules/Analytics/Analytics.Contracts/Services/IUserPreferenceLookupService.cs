namespace Analytics.Contracts.Services;

/// <summary>
/// Cross-module contract for user preference lookup.
/// Consumed by ContentTours search re-ranking behind feature flag analytics.FeatureFlags.PersonalizedSearch.
/// </summary>
public interface IUserPreferenceLookupService
{
    Task<UserPreferenceSummary?> GetPreferenceSummaryAsync(Guid userId, CancellationToken ct = default);
}

public sealed record UserPreferenceSummary(
    string? BudgetTier,
    bool IsFamilyTraveler,
    IReadOnlyList<Guid> PreferredCategoryIds,
    IReadOnlyDictionary<Guid, decimal> CategoryScores);
