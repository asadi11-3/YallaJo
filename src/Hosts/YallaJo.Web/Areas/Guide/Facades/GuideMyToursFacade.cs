using System.Globalization;
using YallaJo.Web.Areas.Guide.ApiClients;
using YallaJo.Web.Areas.Guide.Models.MyTours;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Guide.Facades;

public sealed class GuideMyToursFacade
{
    private const string TimeFormat = "HH:mm";

    private readonly MyToursApiClient _api;
    private readonly ILogger<GuideMyToursFacade> _logger;

    public GuideMyToursFacade(MyToursApiClient api, ILogger<GuideMyToursFacade> logger)
    {
        _api = api;
        _logger = logger;
    }

    public async Task<ApiResult<MyToursVm>> GetMyToursAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var profile = await _api.GetMyProfileAsync(ct);
        if (profile.RequireSignOut)
        {
            return ApiResult<MyToursVm>.ForceSignOut();
        }

        if (!profile.IsSuccess || profile.Data is null)
        {
            return ApiResult<MyToursVm>.Fail(profile.StatusCode, profile.Error);
        }

        var toursResult = await _api.GetMyToursAsync(profile.Data.Id, page, pageSize, ct);
        if (toursResult.RequireSignOut)
        {
            return ApiResult<MyToursVm>.ForceSignOut();
        }

        if (!toursResult.IsSuccess || toursResult.Data is null)
        {
            return ApiResult<MyToursVm>.Fail(toursResult.StatusCode, toursResult.Error);
        }

        var rows = toursResult.Data.Items
            .Select(t => new MyTourRowVm(t.TourId, t.Title, t.Slug, t.IsProposer, t.OfferingStatus, t.OffersPrivateTour, t.AssignedAt))
            .ToList();

        var vm = new MyToursVm
        {
            Tours = rows,
            TotalCount = toursResult.Data.TotalCount,
            Page = page,
            PageSize = pageSize,
        };

        return ApiResult<MyToursVm>.Ok(vm);
    }

    public async Task<ApiResult<OfferingDetailVm>> GetOfferingDetailAsync(Guid tourId, CancellationToken ct = default)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult<OfferingDetailVm>.Fail(404, "Unable to resolve your guide profile.");
        }

        var detail = await _api.GetOfferingDetailAsync(tourId, guideId.Value, ct);
        if (detail.RequireSignOut)
        {
            return ApiResult<OfferingDetailVm>.ForceSignOut();
        }

        if (!detail.IsSuccess || detail.Data is null)
        {
            return ApiResult<OfferingDetailVm>.Fail(detail.StatusCode, detail.Error);
        }

        var d = detail.Data;
        var vm = new OfferingDetailVm
        {
            TourId = d.TourId,
            GuideId = d.TourGuideId,
            Status = d.Status,
            IsProposer = d.IsProposer,
            AssignedAt = d.AssignedAt,
            OffersPrivateTour = d.OffersPrivateTour,
            PrivateTourPriceMultiplier = d.PrivateTourPriceMultiplier,
            PrivateTourFlatPrice = d.PrivateTourFlatPrice,
            Schedules = d.Schedules
                .Select(s => new ScheduleRowVm(
                    s.Id,
                    s.DayOfWeek,
                    s.StartTime.ToString(TimeFormat, CultureInfo.InvariantCulture),
                    s.EndTime?.ToString(TimeFormat, CultureInfo.InvariantCulture),
                    s.IsActive))
                .ToList(),
            PricingTiers = d.PricingTiers
                .Select(p => new PricingTierRowVm(
                    p.Id,
                    p.Name,
                    p.Description,
                    p.Price,
                    p.Currency,
                    p.MinParticipants,
                    p.MaxParticipants,
                    p.IsActive))
                .ToList(),
            PrivateTourForm = new PrivateTourFormVm
            {
                Multiplier = d.PrivateTourPriceMultiplier,
                FlatPrice = d.PrivateTourFlatPrice,
            },
        };

        return ApiResult<OfferingDetailVm>.Ok(vm);
    }

    public Task<ApiResult> AddScheduleAsync(Guid tourId, AddScheduleFormVm form, CancellationToken ct = default) =>
        WithGuideIdAsync((guideId, token) =>
            _api.AddScheduleAsync(tourId, guideId, new CreateScheduleRequest(form.DayOfWeek, form.StartTime, NullIfBlank(form.EndTime)), token), ct);

    public Task<ApiResult> DeleteScheduleAsync(Guid tourId, Guid scheduleId, CancellationToken ct = default) =>
        WithGuideIdAsync((guideId, token) => _api.DeleteScheduleAsync(tourId, guideId, scheduleId, token), ct);

    public Task<ApiResult> AddPricingTierAsync(Guid tourId, AddPricingTierFormVm form, CancellationToken ct = default) =>
        WithGuideIdAsync((guideId, token) =>
            _api.AddPricingTierAsync(tourId, guideId, new CreatePricingTierRequest(
                form.Name, form.Price, form.Currency, form.MinParticipants, form.MaxParticipants, NullIfBlank(form.Description)), token), ct);

    public Task<ApiResult> DeletePricingTierAsync(Guid tourId, Guid tierId, CancellationToken ct = default) =>
        WithGuideIdAsync((guideId, token) => _api.DeletePricingTierAsync(tourId, guideId, tierId, token), ct);

    public Task<ApiResult> EnablePrivateTourAsync(Guid tourId, PrivateTourFormVm form, CancellationToken ct = default) =>
        WithGuideIdAsync((guideId, token) =>
            _api.EnablePrivateTourAsync(tourId, guideId, new EnablePrivateTourRequest(form.Multiplier, form.FlatPrice), token), ct);

    public Task<ApiResult> DisablePrivateTourAsync(Guid tourId, CancellationToken ct = default) =>
        WithGuideIdAsync((guideId, token) => _api.DisablePrivateTourAsync(tourId, guideId, token), ct);

    public Task<ApiResult> RemoveOfferingAsync(Guid tourId, CancellationToken ct = default) =>
        WithGuideIdAsync((guideId, token) => _api.RemoveOfferingAsync(tourId, guideId, token), ct);

    private async Task<ApiResult> WithGuideIdAsync(Func<Guid, CancellationToken, Task<ApiResult>> action, CancellationToken ct)
    {
        var guideId = await ResolveGuideIdAsync(ct);
        if (guideId is null)
        {
            return ApiResult.Fail("Unable to resolve your guide profile.");
        }

        return await action(guideId.Value, ct);
    }

    private async Task<Guid?> ResolveGuideIdAsync(CancellationToken ct)
    {
        try
        {
            var profile = await _api.GetMyProfileAsync(ct);
            return profile is { IsSuccess: true, Data: not null } ? profile.Data.Id : null;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to resolve guide id for offering mutation.");
            return null;
        }
    }

    private static string? NullIfBlank(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
