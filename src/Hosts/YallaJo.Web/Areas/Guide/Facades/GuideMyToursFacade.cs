using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideMyToursFacade
{
    private const int PageSize = 20;
    private readonly GuideApiClient _api;

    public GuideMyToursFacade(GuideApiClient api) => _api = api;

    public async Task<ApiResult<GuideMyToursVm>> GetAsync(int page, CancellationToken ct = default)
    {
        if (page < 1)
        {
            page = 1;
        }

        var profile = await _api.GetMyProfileAsync(ct);
        if (profile.IsUnauthorized)
        {
            return ApiResult<GuideMyToursVm>.ForceSignOut();
        }

        if (profile is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<GuideMyToursVm>.Ok(new GuideMyToursVm { IsGuide = false });
        }

        var vm = new GuideMyToursVm { IsGuide = true };

        try
        {
            var tours = await _api.GetGuideToursAsync(profile.Data.Id, page, PageSize, ct);
            if (tours is { IsSuccess: true, Data: not null })
            {
                vm.TotalCount = tours.Data.TotalCount;
                vm.Tours = tours.Data.Items
                    .Select(t => new GuideTourRowVm
                    {
                        TourId = t.TourId,
                        Title = t.Title,
                        Slug = t.Slug,
                        OfferingStatus = t.OfferingStatus,
                        IsProposer = t.IsProposer,
                        OffersPrivateTour = t.OffersPrivateTour,
                        AssignedAt = t.AssignedAt,
                    })
                    .ToList();
            }
        }
        catch
        {
            // Tolerate hydration failure: render the page with an empty list.
        }

        return ApiResult<GuideMyToursVm>.Ok(vm);
    }
}
