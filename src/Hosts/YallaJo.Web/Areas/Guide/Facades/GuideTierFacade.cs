using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

/// <summary>
/// Backs the standalone §5.5 Tier page. Reuses <see cref="DashboardApiClient"/>'s
/// <c>GET /api/v1/guides/me/tier</c> binding rather than duplicating an ApiClient.
/// </summary>
public sealed class GuideTierFacade
{
    private readonly DashboardApiClient _api;

    public GuideTierFacade(DashboardApiClient api) => _api = api;

    public async Task<ApiResult<TierProgressVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetTierProgressAsync(ct);
        if (result.RequireSignOut)
        {
            return ApiResult<TierProgressVm>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<TierProgressVm>.Fail(result.StatusCode, result.Error ?? "Could not load your tier progress.");
        }

        var t = result.Data;
        return ApiResult<TierProgressVm>.Ok(new TierProgressVm
        {
            CurrentTier = t.CurrentTier,
            NextTier = t.NextTier,
            CompletedTours = t.CompletedTours,
            CompletedToursRequired = t.CompletedToursRequired,
            AverageRating = t.AverageRating,
            AverageRatingRequired = t.AverageRatingRequired,
            ReportRate = t.ReportRate,
            MaxReportRateAllowed = t.MaxReportRateAllowed,
            ActiveMonths = t.ActiveMonths,
            ActiveMonthsRequired = t.ActiveMonthsRequired,
            CurrentCommissionRate = t.CurrentCommissionRate,
        });
    }
}
