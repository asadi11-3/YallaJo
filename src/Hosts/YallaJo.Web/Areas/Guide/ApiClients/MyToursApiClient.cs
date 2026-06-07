using YallaJo.Web.Areas.Guide.Models.Dashboard;
using YallaJo.Web.Areas.Guide.Models.MyTours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Guide.ApiClients;

public sealed class MyToursApiClient
{
    private const string GuidesBase = "/api/v1/guides";
    private const string ToursBase = "/api/v1/tours";

    private readonly IApiClient _api;

    public MyToursApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<TourGuideProfileResponse>> GetMyProfileAsync(CancellationToken ct = default) =>
        _api.GetAsync<TourGuideProfileResponse>($"{GuidesBase}/me", ct);

    public Task<ApiResult<GuideToursResponse>> GetMyToursAsync(Guid guideId, int page, int pageSize, CancellationToken ct = default) =>
        _api.GetAsync<GuideToursResponse>($"{GuidesBase}/{guideId}/tours?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<GuideOfferingDetailDto>> GetOfferingDetailAsync(Guid tourId, Guid guideId, CancellationToken ct = default) =>
        _api.GetAsync<GuideOfferingDetailDto>($"{ToursBase}/{tourId}/guide-offerings/{guideId}", ct);

    // Schedules
    public Task<ApiResult> AddScheduleAsync(Guid tourId, Guid guideId, CreateScheduleRequest req, CancellationToken ct = default) =>
        _api.PostAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/schedules", req, ct);

    public Task<ApiResult> UpdateScheduleAsync(Guid tourId, Guid guideId, Guid scheduleId, UpdateScheduleRequest req, CancellationToken ct = default) =>
        _api.PutAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/schedules/{scheduleId}", req, ct);

    public Task<ApiResult> DeleteScheduleAsync(Guid tourId, Guid guideId, Guid scheduleId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/schedules/{scheduleId}", ct);

    // Pricing tiers
    public Task<ApiResult> AddPricingTierAsync(Guid tourId, Guid guideId, CreatePricingTierRequest req, CancellationToken ct = default) =>
        _api.PostAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/pricing-tiers", req, ct);

    public Task<ApiResult> UpdatePricingTierAsync(Guid tourId, Guid guideId, Guid tierId, UpdatePricingTierRequest req, CancellationToken ct = default) =>
        _api.PutAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/pricing-tiers/{tierId}", req, ct);

    public Task<ApiResult> DeletePricingTierAsync(Guid tourId, Guid guideId, Guid tierId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/pricing-tiers/{tierId}", ct);

    // Private tour
    public Task<ApiResult> EnablePrivateTourAsync(Guid tourId, Guid guideId, EnablePrivateTourRequest req, CancellationToken ct = default) =>
        _api.PostAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/private-tour", req, ct);

    public Task<ApiResult> DisablePrivateTourAsync(Guid tourId, Guid guideId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}/private-tour", ct);

    // DELETE /api/v1/tours/{tourId}/guide-offerings/{guideId} — remove the whole offering
    public Task<ApiResult> RemoveOfferingAsync(Guid tourId, Guid guideId, CancellationToken ct = default) =>
        _api.DeleteAsync($"{ToursBase}/{tourId}/guide-offerings/{guideId}", ct);
}
