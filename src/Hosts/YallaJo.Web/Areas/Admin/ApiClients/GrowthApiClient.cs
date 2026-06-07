using System.Globalization;
using YallaJo.Web.Areas.Admin.Models.Growth;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// §8.8 Growth tools — Analytics admin endpoints under /api/v1/analytics/admin
/// (seasonality rules, holiday calendar, photogenic flags, A/B experiments, segments).
/// </summary>
public sealed class GrowthApiClient
{
    private const string Base = "/api/v1/analytics/admin";

    private readonly IApiClient _api;

    public GrowthApiClient(IApiClient api) => _api = api;

    // ── Seasonality ───────────────────────────────────────────────────────────
    public Task<ApiResult<List<SeasonalityRuleResponse>>> GetSeasonalityRulesAsync(CancellationToken ct = default)
        => _api.GetAsync<List<SeasonalityRuleResponse>>($"{Base}/seasonality/", ct);

    public Task<ApiResult> CreateSeasonalityRuleAsync(CreateSeasonalityRuleApiRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/seasonality/", request, ct);

    public Task<ApiResult> DeactivateSeasonalityRuleAsync(Guid ruleId, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/seasonality/{ruleId:D}", ct);

    // ── Holiday calendar ────────────────────────────────────────────────────────
    public Task<ApiResult<List<HolidayCalendarResponse>>> GetHolidaysAsync(int year, CancellationToken ct = default)
        => _api.GetAsync<List<HolidayCalendarResponse>>(
            $"{Base}/holidays/{year.ToString(CultureInfo.InvariantCulture)}", ct);

    public Task<ApiResult> CreateHolidayAsync(CreateHolidayCalendarApiRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/holidays/", request, ct);

    // ── Photogenic ────────────────────────────────────────────────────────────
    public Task<ApiResult> SetPhotogenicAsync(string kind, Guid entityId, bool isPhotogenic, CancellationToken ct = default)
        => _api.PutAsync(
            $"{Base}/entities/{Uri.EscapeDataString(kind)}/{entityId:D}/photogenic",
            new SetPhotogenicApiRequest(isPhotogenic),
            ct);

    // ── A/B experiments ─────────────────────────────────────────────────────────
    public Task<ApiResult> CreateExperimentAsync(CreateExperimentApiRequest request, CancellationToken ct = default)
        => _api.PostAsync($"{Base}/experiments/", request, ct);

    public Task<ApiResult> StartExperimentAsync(Guid experimentId, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/experiments/{experimentId:D}/start", null, ct);

    public Task<ApiResult> CompleteExperimentAsync(Guid experimentId, CancellationToken ct = default)
        => _api.PutAsync($"{Base}/experiments/{experimentId:D}/complete", null, ct);

    // ── Re-engagement segments ───────────────────────────────────────────────────
    public Task<ApiResult<string>> GetSegmentAsync(
        string rule, string? entityKind, Guid? entityId, CancellationToken ct = default)
    {
        var query = new List<string> { "rule=" + Uri.EscapeDataString(rule) };
        if (!string.IsNullOrWhiteSpace(entityKind))
        {
            query.Add("entityKind=" + Uri.EscapeDataString(entityKind));
        }

        if (entityId is { } id && id != Guid.Empty)
        {
            query.Add("entityId=" + id.ToString("D"));
        }

        return _api.GetAsync<string>($"{Base}/segments/?" + string.Join("&", query), ct);
    }
}
