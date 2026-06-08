using Microsoft.AspNetCore.OutputCaching;
using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Growth;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// §8.8 Growth tools orchestration. Aggregates the seasonality + holiday lists for the
/// page, and forwards each admin mutation to <see cref="GrowthApiClient"/> with a
/// consistent <see cref="ApiResult"/> mapping.
/// </summary>
public sealed class GrowthFacade
{
    private readonly GrowthApiClient _api;
    private readonly IOutputCacheStore _cache;

    public GrowthFacade(GrowthApiClient api, IOutputCacheStore cache)
    {
        _api = api;
        _cache = cache;
    }

    public async Task<ApiResult<GrowthVm>> GetIndexAsync(int? year, CancellationToken ct = default)
    {
        var targetYear = year is < 2000 or > 2100 ? DateTime.UtcNow.Year : year ?? DateTime.UtcNow.Year;

        var rulesTask = _api.GetSeasonalityRulesAsync(ct);
        var holidaysTask = _api.GetHolidaysAsync(targetYear, ct);
        await Task.WhenAll(rulesTask, holidaysTask);

        var rules = await rulesTask;
        var holidays = await holidaysTask;

        if (rules.IsUnauthorized || holidays.IsUnauthorized)
        {
            return ApiResult<GrowthVm>.ForceSignOut();
        }

        var vm = new GrowthVm
        {
            Year = targetYear,
            SeasonalityRules = rules is { IsSuccess: true, Data: not null } ? rules.Data : [],
            Holidays = holidays is { IsSuccess: true, Data: not null } ? holidays.Data : [],
            SeasonalityLoadFailed = rules is not { IsSuccess: true },
            HolidaysLoadFailed = holidays is not { IsSuccess: true },
        };

        return ApiResult<GrowthVm>.Ok(vm);
    }

    // ── Seasonality ───────────────────────────────────────────────────────────
    public Task<ApiResult> CreateSeasonalityRuleAsync(CreateSeasonalityRuleVm form, CancellationToken ct = default)
    {
        if (form.PlaceId == Guid.Empty)
        {
            return Task.FromResult(ApiResult.Fail(400, "A place is required."));
        }

        if (form.MonthStart is < 1 or > 12 || form.MonthEnd is < 1 or > 12)
        {
            return Task.FromResult(ApiResult.Fail(400, "Months must be between 1 and 12."));
        }

        if (form.Multiplier <= 0m)
        {
            return Task.FromResult(ApiResult.Fail(400, "Multiplier must be greater than zero."));
        }

        var request = new CreateSeasonalityRuleApiRequest(
            form.PlaceId, form.MonthStart, form.MonthEnd, form.Multiplier,
            string.IsNullOrWhiteSpace(form.Description) ? null : form.Description.Trim());

        return Normalize(_api.CreateSeasonalityRuleAsync(request, ct), "Could not create the seasonality rule.");
    }

    public Task<ApiResult> DeactivateSeasonalityRuleAsync(Guid ruleId, CancellationToken ct = default)
        => ruleId == Guid.Empty
            ? Task.FromResult(ApiResult.Fail(400, "A valid rule is required."))
            : Normalize(_api.DeactivateSeasonalityRuleAsync(ruleId, ct), "Could not deactivate the seasonality rule.");

    // ── Holiday calendar ────────────────────────────────────────────────────────
    public Task<ApiResult> CreateHolidayAsync(CreateHolidayVm form, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(form.HolidayName))
        {
            return Task.FromResult(ApiResult.Fail(400, "A holiday name is required."));
        }

        if (form.EndDate < form.StartDate)
        {
            return Task.FromResult(ApiResult.Fail(400, "The end date must be on or after the start date."));
        }

        var request = new CreateHolidayCalendarApiRequest(
            form.HolidayName.Trim(), form.StartDate, form.EndDate, form.Year,
            string.IsNullOrWhiteSpace(form.BoostRulesJson) ? null : form.BoostRulesJson.Trim());

        return Normalize(_api.CreateHolidayAsync(request, ct), "Could not create the holiday entry.");
    }

    // ── Photogenic ────────────────────────────────────────────────────────────
    public Task<ApiResult> SetPhotogenicAsync(string? kind, Guid entityId, bool isPhotogenic, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(kind) || entityId == Guid.Empty)
        {
            return Task.FromResult(ApiResult.Fail(400, "An entity kind and id are required."));
        }

        return Normalize(
            _api.SetPhotogenicAsync(kind.Trim(), entityId, isPhotogenic, ct),
            "Could not update the photogenic flag.",
            PhotogenicTags(kind, entityId));
    }

    // §8.8 — toggling the photogenic flag changes the entity's public detail page and the
    // homepage rails it can surface in. Evict the entity tag (when kind is tour/place) plus
    // homepage so the cached public pages reflect the change immediately.
    private static string[] PhotogenicTags(string? kind, Guid entityId)
    {
        if (string.Equals(kind, "tour", StringComparison.OrdinalIgnoreCase))
            return ["homepage", $"tour:{entityId}"];
        if (string.Equals(kind, "place", StringComparison.OrdinalIgnoreCase))
            return ["homepage", $"place:{entityId}"];
        return ["homepage"];
    }

    // ── A/B experiments ─────────────────────────────────────────────────────────
    public Task<ApiResult> CreateExperimentAsync(CreateExperimentVm form, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(form.Name))
        {
            return Task.FromResult(ApiResult.Fail(400, "An experiment name is required."));
        }

        if (form.ExpiresAt <= form.StartsAt)
        {
            return Task.FromResult(ApiResult.Fail(400, "The end date must be after the start date."));
        }

        if (form.TrafficPercent is < 0 or > 100)
        {
            return Task.FromResult(ApiResult.Fail(400, "Traffic percent must be between 0 and 100."));
        }

        var request = new CreateExperimentApiRequest(
            form.Name.Trim(), form.StartsAt, form.ExpiresAt, form.TrafficPercent,
            string.IsNullOrWhiteSpace(form.VariantsJson) ? "[]" : form.VariantsJson.Trim());

        return Normalize(_api.CreateExperimentAsync(request, ct), "Could not create the experiment.");
    }

    public Task<ApiResult> StartExperimentAsync(Guid experimentId, CancellationToken ct = default)
        => experimentId == Guid.Empty
            ? Task.FromResult(ApiResult.Fail(400, "A valid experiment is required."))
            : Normalize(_api.StartExperimentAsync(experimentId, ct), "Could not start the experiment.");

    public Task<ApiResult> CompleteExperimentAsync(Guid experimentId, CancellationToken ct = default)
        => experimentId == Guid.Empty
            ? Task.FromResult(ApiResult.Fail(400, "A valid experiment is required."))
            : Normalize(_api.CompleteExperimentAsync(experimentId, ct), "Could not complete the experiment.");

    // ── Segments ──────────────────────────────────────────────────────────────
    public async Task<ApiResult<string>> GetSegmentAsync(
        string? rule, string? entityKind, Guid? entityId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(rule))
        {
            return ApiResult<string>.CreateFailure("A segment rule is required.");
        }

        var result = await _api.GetSegmentAsync(rule.Trim(), entityKind, entityId, ct);
        if (result.IsSuccess)
        {
            return ApiResult<string>.CreateSuccess(result.Data ?? string.Empty);
        }

        return result.IsUnauthorized
            ? ApiResult<string>.ForceSignOut()
            : ApiResult<string>.CreateFailure(result.Error ?? "Could not query the segment.");
    }

    // Evicts the supplied public output-cache tags on a successful write so cached public
    // pages reflect the change immediately (§8.8 C3). CancellationToken.None ensures the
    // eviction still runs even if the admin client disconnected after the backend committed.
    private async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback, params string[] evictTags)
    {
        var result = await call;
        if (result.IsSuccess)
        {
            foreach (var tag in evictTags)
            {
                await _cache.EvictByTagAsync(tag, CancellationToken.None);
            }
            return ApiResult.Ok();
        }
        if (result.IsUnauthorized) return ApiResult.ForceSignOut();
        if (result.IsNotFound) return ApiResult.Fail(404, "The requested item was not found.");
        if (result.IsConflict) return ApiResult.Fail(409, result.Error ?? "This action is not allowed in the current state.");
        if (result.IsValidationError && result.ValidationErrors is not null) return ApiResult.Invalid(result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
