using YallaJo.Web.Areas.Provider.Models.TourWaypoints;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderTourWaypointsApiClient
{
    private readonly IApiClient _api;

    public ProviderTourWaypointsApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/{id}/waypoints
    public Task<ApiResult<List<TourWaypointResponse>>> GetWaypointsAsync(Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<List<TourWaypointResponse>>($"/api/v1/tours/{tourId}/waypoints", ct);

    // POST /api/v1/tours/{id}/waypoints
    public Task<ApiResult<CreateTourWaypointResponse>> CreateAsync(
        Guid tourId, CreateTourWaypointApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateTourWaypointResponse>($"/api/v1/tours/{tourId}/waypoints", request, ct);

    // PUT /api/v1/tours/{id}/waypoints/{waypointId}
    public Task<ApiResult> UpdateAsync(
        Guid tourId, Guid waypointId, UpdateTourWaypointApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/tours/{tourId}/waypoints/{waypointId}", request, ct);

    // DELETE /api/v1/tours/{id}/waypoints/{waypointId}
    public Task<ApiResult> DeleteAsync(Guid tourId, Guid waypointId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/tours/{tourId}/waypoints/{waypointId}", ct);

    // PUT /api/v1/tours/{id}/waypoints/reorder
    public Task<ApiResult> ReorderAsync(
        Guid tourId, ReorderTourWaypointsApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/tours/{tourId}/waypoints/reorder", request, ct);
}
