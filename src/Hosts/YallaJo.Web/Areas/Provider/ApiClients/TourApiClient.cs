using YallaJo.Web.Areas.Provider.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class TourApiClient
{
    private readonly IApiClient _api;

    public TourApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<CreateTourResultResponse>> CreateAsync(CreateTourRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateTourResultResponse>("/api/v1/tours", request, ct);

    public Task<ApiResult<TourDetailResponse>> GetTourAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourDetailResponse>($"/api/v1/tours/{id}", ct);

    public Task<ApiResult<List<TourScheduleResponse>>> GetSchedulesAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<TourScheduleResponse>>($"/api/v1/tours/{id}/schedules?activeOnly=false", ct);

    public Task<ApiResult> AddScheduleAsync(Guid id, CreateTourScheduleRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{id}/schedules", request, ct);

    public Task<ApiResult> DeleteScheduleAsync(Guid id, Guid scheduleId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/tours/{id}/schedules/{scheduleId}", ct);

    public Task<ApiResult<List<TourPricingTierResponse>>> GetPricingAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<TourPricingTierResponse>>($"/api/v1/tours/{id}/pricing?activeOnly=false", ct);

    public Task<ApiResult> AddPricingAsync(Guid id, CreateTourPricingTierRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{id}/pricing", request, ct);

    public Task<ApiResult> DeletePricingAsync(Guid id, Guid tierId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/tours/{id}/pricing/{tierId}", ct);

    public Task<ApiResult<List<TourWaypointResponse>>> GetWaypointsAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<List<TourWaypointResponse>>($"/api/v1/tours/{id}/waypoints", ct);

    public Task<ApiResult> AddWaypointAsync(Guid id, AddTourWaypointRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{id}/waypoints", request, ct);

    public Task<ApiResult> DeleteWaypointAsync(Guid id, Guid waypointId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/tours/{id}/waypoints/{waypointId}", ct);

    public Task<ApiResult<ChildrenInfoResponse>> GetChildrenInfoAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<ChildrenInfoResponse>($"/api/v1/tours/{id}/children-info", ct);

    public Task<ApiResult> UpdateChildrenInfoAsync(Guid id, UpdateChildrenInfoRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/tours/{id}/children-info", request, ct);
}
