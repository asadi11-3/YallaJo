using Microsoft.Extensions.Logging;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Recommendations;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class RecommendationsFacade
{
    private const int FeedLimit = 20;
    private readonly RecommendationsApiClient _api;
    private readonly ILogger<RecommendationsFacade> _logger;

    public RecommendationsFacade(RecommendationsApiClient api, ILogger<RecommendationsFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<RecommendationsVm>> GetAsync(CancellationToken ct = default)
    {
        // Primary call: the personalized feed (requires auth).
        var feed = await _api.GetRecommendationsAsync(FeedLimit, ct: ct);
        if (feed.RequireSignOut)
        {
            return ApiResult<RecommendationsVm>.ForceSignOut();
        }
        if (feed is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<RecommendationsVm>.Fail(
                feed.StatusCode,
                feed.Error ?? "Could not load your recommendations.");
        }

        // Secondary: preferences (non-blocking; defaults to empty form on failure).
        var preferences = await SafePreferencesAsync(ct);

        var vm = new RecommendationsVm
        {
            IsPersonalized = feed.Data.IsPersonalized,
            ComputedAt = feed.Data.ComputedAt,
            Recommendations = RecommendationsMapper.ToRows(feed.Data.Items),
            Preferences = RecommendationsMapper.ToPreferencesForm(preferences),
        };
        return ApiResult<RecommendationsVm>.Ok(vm);
    }

    public Task<ApiResult> UpdatePreferencesAsync(PreferencesFormVm form, CancellationToken ct = default)
    {
        var request = new UpdatePreferencesApiRequest(
            NullIfBlank(form.BudgetTier),
            form.IsFamilyTraveler,
            NullIfBlank(form.CurrentTripStage),
            Array.Empty<UpdatePreferredCategoryApiRequest>());
        return NormalizeAsync(_api.UpdatePreferencesAsync(request, ct), "Could not save your preferences.");
    }

    public Task<ApiResult> MarkNotInterestedAsync(
        RecommendationEntityType kind,
        Guid entityId,
        CancellationToken ct = default) =>
        NormalizeAsync(
            _api.MarkNotInterestedAsync(new MarkNotInterestedApiRequest(kind, entityId), ct),
            "Could not update this recommendation.");

    public Task<ApiResult> RecordInteractionAsync(
        string entityType,
        Guid entityId,
        string interactionType,
        CancellationToken ct = default) =>
        NormalizeAsync(
            _api.RecordInteractionAsync(
                new RecordInteractionApiRequest(null, null, entityType, entityId, interactionType), ct),
            "Could not record the interaction.");

    public Task<ApiResult> SubmitOnboardingAsync(OnboardingFormVm form, CancellationToken ct = default)
    {
        var interested = form.Interested
            .Select(OnboardingFormVm.ParseToken)
            .Where(r => r is not null)
            .Select(r => new OnboardingEntityRefApiRequest(r!.Value.Kind, r.Value.EntityId))
            .ToList();

        var notInterested = form.NotInterested
            .Select(OnboardingFormVm.ParseToken)
            .Where(r => r is not null)
            .Select(r => new OnboardingEntityRefApiRequest(r!.Value.Kind, r.Value.EntityId))
            .ToList();

        var request = new OnboardingApiRequest(interested, notInterested);
        return NormalizeAsync(_api.SubmitOnboardingAsync(request, ct), "Could not save your onboarding answers.");
    }

    private async Task<UserPreferencesResponse?> SafePreferencesAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPreferencesAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to load personalization preferences");
            return null;
        }
    }

    private async Task<ApiResult> NormalizeAsync(Task<ApiResult> call, string fallback)
    {
        try
        {
            var result = await call;
            if (result.IsSuccess) return ApiResult.Ok();
            if (result.IsUnauthorized) return ApiResult.ForceSignOut();
            if (result.IsValidationError && result.ValidationErrors is not null)
            {
                return ApiResult.Invalid(result.ValidationErrors);
            }
            return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recommendation mutation failed");
            return ApiResult.Fail(500, fallback);
        }
    }

    private async Task<ApiResult> NormalizeAsync<T>(Task<ApiResult<T>> call, string fallback)
    {
        try
        {
            var result = await call;
            if (result.IsSuccess) return ApiResult.Ok();
            if (result.IsUnauthorized) return ApiResult.ForceSignOut();
            if (result.IsValidationError && result.ValidationErrors is not null)
            {
                return ApiResult.Invalid(result.ValidationErrors);
            }
            return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Recommendation mutation failed");
            return ApiResult.Fail(500, fallback);
        }
    }

    private static string? NullIfBlank(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
