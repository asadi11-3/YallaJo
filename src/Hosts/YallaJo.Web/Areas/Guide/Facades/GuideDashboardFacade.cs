using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideDashboardFacade
{
    private readonly DashboardApiClient _api;

    public GuideDashboardFacade(DashboardApiClient api) => _api = api;

    public async Task<ApiResult<DashboardVm>> GetDashboardAsync(CancellationToken ct = default)
    {
        var profileResult = await _api.GetMyProfileAsync(ct);
        if (profileResult.RequireSignOut)
        {
            return ApiResult<DashboardVm>.ForceSignOut();
        }

        var profile = profileResult is { IsSuccess: true, Data: not null } ? profileResult.Data : null;

        var earningsTask = SafeEarningsAsync(ct);
        var blocksTask = SafeAvailabilityBlocksAsync(ct);
        var tierTask = SafeTierProgressAsync(ct);
        var toursTask = profile is null
            ? Task.FromResult(new GuideToursResponse())
            : SafeToursAsync(profile.Id, ct);

        await Task.WhenAll(earningsTask, blocksTask, tierTask, toursTask);

        var earnings = earningsTask.Result;
        var blocks = blocksTask.Result;
        var tier = tierTask.Result;
        var tours = toursTask.Result;

        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var activeBlocks = blocks.Count(b => b.EndDate >= today);

        var recentTours = tours.Items
            .OrderByDescending(t => t.AssignedAt ?? DateTime.MinValue)
            .Take(5)
            .Select(t => new RecentTourVm
            {
                TourId = t.TourId,
                Title = t.Title,
                Slug = t.Slug,
                OfferingStatus = t.OfferingStatus,
                OffersPrivateTour = t.OffersPrivateTour,
                AssignedAt = t.AssignedAt,
            })
            .ToList();

        var vm = new DashboardVm
        {
            DisplayName = profile?.DisplayName ?? string.Empty,
            AvatarUrl = profile?.AvatarUrl,
            AverageRating = profile?.AverageRating ?? 0m,
            ReviewCount = profile?.ReviewCount ?? 0,
            TourCount = profile?.TourCount ?? tours.TotalCount,
            YearsOfExperience = profile?.YearsOfExperience ?? 0,
            HasFirstAid = profile?.HasFirstAid ?? false,
            TotalEarned = earnings?.TotalEarned ?? 0m,
            ThisMonth = earnings?.ThisMonth ?? 0m,
            PendingPayout = earnings?.PendingPayout ?? 0m,
            NetEarnings = earnings?.NetEarnings ?? 0m,
            EarningsCurrency = earnings?.Currency ?? string.Empty,
            ActiveAvailabilityBlocks = activeBlocks,
            RecentTours = recentTours,
            TierProgress = tier is null ? null : new TierProgressVm
            {
                CurrentTier = tier.CurrentTier,
                NextTier = tier.NextTier,
                CompletedTours = tier.CompletedTours,
                CompletedToursRequired = tier.CompletedToursRequired,
                AverageRating = tier.AverageRating,
                AverageRatingRequired = tier.AverageRatingRequired,
                ReportRate = tier.ReportRate,
                MaxReportRateAllowed = tier.MaxReportRateAllowed,
                ActiveMonths = tier.ActiveMonths,
                ActiveMonthsRequired = tier.ActiveMonthsRequired,
                CurrentCommissionRate = tier.CurrentCommissionRate,
            },
        };

        return ApiResult<DashboardVm>.Ok(vm);
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

    private async Task<List<GuideAvailabilityBlockResponse>> SafeAvailabilityBlocksAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetAvailabilityBlocksAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : [];
        }
        catch
        {
            return [];
        }
    }

    private async Task<GuideTierProgressResponse?> SafeTierProgressAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetTierProgressAsync(ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : null;
        }
        catch
        {
            return null;
        }
    }

    private async Task<GuideToursResponse> SafeToursAsync(Guid guideId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetMyToursAsync(guideId, 1, 5, ct);
            return result is { IsSuccess: true, Data: not null } ? result.Data : new GuideToursResponse();
        }
        catch
        {
            return new GuideToursResponse();
        }
    }
}
