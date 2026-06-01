using Analytics.Application.Interfaces;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Application.Queries.GetUserPreferences;
using Analytics.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Logging;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Commands.SetUserPreferences;

public sealed class SetUserPreferencesCommandHandler(
    IUserPreferenceRepository repository,
    IAnalyticsUnitOfWork unitOfWork,
    HybridCache cache,
    ILogger<SetUserPreferencesCommandHandler> logger) : ICommandHandler<SetUserPreferencesCommand, UserPreferencesResponse>
{
    public async Task<Result<UserPreferencesResponse>> Handle(SetUserPreferencesCommand request, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var preference = await repository.GetByUserIdAsync(request.UserId, ct).ConfigureAwait(false);
        if (preference is null)
        {
            preference = UserPreference.Create(request.UserId, request.BudgetTier, request.IsFamilyTraveler, request.CurrentTripStage, now);
            await repository.AddAsync(preference, ct).ConfigureAwait(false);
        }
        else
        {
            preference.Update(request.BudgetTier, request.IsFamilyTraveler, request.CurrentTripStage, now);
        }

        foreach (var category in request.Categories)
        {
            await repository.UpsertPreferredCategoryAsync(UserPreferredCategory.Create(request.UserId, category.CategoryId, category.PreferenceScore), ct).ConfigureAwait(false);
        }

        try
        {
            await unitOfWork.SaveChangesAsync(ct).ConfigureAwait(false);
        }
        catch (DbUpdateConcurrencyException)
        {
            return Result<UserPreferencesResponse>.Failure(
                new Error("UserPreference.ConcurrencyConflict", "User preferences were modified concurrently. Please refresh and try again."),
                Outcome.Conflict);
        }

        await cache.RemoveByTagAsync($"analytics:prefs:{request.UserId:N}", ct).ConfigureAwait(false);
        await cache.RemoveByTagAsync($"analytics:recs:{request.UserId:N}", ct).ConfigureAwait(false);

        var categories = await repository.GetPreferredCategoriesAsync(request.UserId, ct).ConfigureAwait(false);
        logger.LogInformation("Updated analytics preferences for user {UserId} with {CategoryCount} preferred categories", request.UserId, categories.Count);

        return Result<UserPreferencesResponse>.Success(new UserPreferencesResponse(
            request.UserId,
            preference.BudgetTier,
            preference.IsFamilyTraveler,
            preference.CurrentTripStage,
            preference.LastComputedAt,
            categories.Select(category => new UserPreferredCategoryResponse(category.CategoryId, category.PreferenceScore, category.UpdatedAt)).ToList()));
    }
}
