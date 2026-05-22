using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetUserPreferences;

public sealed record GetUserPreferencesQuery(Guid UserId) : IQuery<UserPreferencesResponse>, ICacheableQuery
{
    public string CacheKey => $"ct:analytics:prefs:{UserId:N}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(10);
    public IReadOnlyList<string> Tags => [$"analytics:prefs:{UserId:N}"];
}

public sealed record UserPreferencesResponse(
    Guid UserId,
    string? BudgetTier,
    bool IsFamilyTraveler,
    string? CurrentTripStage,
    DateTime? LastComputedAt,
    IReadOnlyList<UserPreferredCategoryResponse> Categories);

public sealed record UserPreferredCategoryResponse(Guid CategoryId, decimal PreferenceScore, DateTime UpdatedAt);

public sealed class GetUserPreferencesQueryHandler(
    IUserPreferenceRepository repository,
    ILogger<GetUserPreferencesQueryHandler> logger) : IQueryHandler<GetUserPreferencesQuery, UserPreferencesResponse>
{
    public async Task<Result<UserPreferencesResponse>> Handle(GetUserPreferencesQuery request, CancellationToken ct)
    {
        var preference = await repository.GetByUserIdAsync(request.UserId, ct).ConfigureAwait(false);
        var categories = await repository.GetPreferredCategoriesAsync(request.UserId, ct).ConfigureAwait(false);

        logger.LogInformation("Loaded analytics preferences for user {UserId} with {CategoryCount} preferred categories", request.UserId, categories.Count);
        return Result<UserPreferencesResponse>.Success(MapPreference(request.UserId, preference, categories));
    }

    private static UserPreferencesResponse MapPreference(Guid userId, UserPreference? preference, IReadOnlyList<UserPreferredCategory> categories) =>
        new(
            userId,
            preference?.BudgetTier,
            preference?.IsFamilyTraveler ?? false,
            preference?.CurrentTripStage,
            preference?.LastComputedAt,
            categories.Select(category => new UserPreferredCategoryResponse(category.CategoryId, category.PreferenceScore, category.UpdatedAt)).ToList());
}
