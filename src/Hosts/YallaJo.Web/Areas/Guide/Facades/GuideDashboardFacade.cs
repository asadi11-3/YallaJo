using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideDashboardFacade
{
    private readonly GuideApiClient _api;

    public GuideDashboardFacade(GuideApiClient api) => _api = api;

    public async Task<ApiResult<GuideDashboardVm>> GetDashboardAsync(CancellationToken ct = default)
    {
        var profile = await _api.GetMyProfileAsync(ct);
        if (profile.IsUnauthorized)
        {
            return ApiResult<GuideDashboardVm>.ForceSignOut();
        }

        // Not a guide (no profile row) -> show empty-state, not an error.
        if (profile is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<GuideDashboardVm>.Ok(new GuideDashboardVm { IsGuide = false });
        }

        var p = profile.Data;
        var vm = new GuideDashboardVm
        {
            IsGuide = true,
            DisplayName = p.DisplayName,
            TourCount = p.TourCount,
            AverageRating = p.AverageRating,
            ReviewCount = p.ReviewCount,
            YearsOfExperience = p.YearsOfExperience,
        };

        var earningsTask = SafeEarningsAsync(ct);
        var toursTask = SafeToursAsync(p.Id, ct);
        await Task.WhenAll(earningsTask, toursTask);

        var earnings = earningsTask.Result;
        if (earnings is not null)
        {
            vm.NetEarnings = earnings.NetEarnings;
            vm.ThisMonth = earnings.ThisMonth;
            vm.PendingPayout = earnings.PendingPayout;
            vm.EarningsCurrency = earnings.Currency;
        }

        vm.RecentTours = toursTask.Result
            .Take(5)
            .Select(t => new GuideTourCardVm
            {
                TourId = t.TourId,
                Title = t.Title,
                Slug = t.Slug,
                OfferingStatus = t.OfferingStatus,
                IsProposer = t.IsProposer,
            })
            .ToList();

        return ApiResult<GuideDashboardVm>.Ok(vm);
    }

    private async Task<GuideEarningsSummaryResponse?> SafeEarningsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetEarningsSummaryAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<GuideTourItemResponse>> SafeToursAsync(Guid guideId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetGuideToursAsync(guideId, 1, 5, ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data.Items : [];
        }
        catch
        {
            return [];
        }
    }
}
