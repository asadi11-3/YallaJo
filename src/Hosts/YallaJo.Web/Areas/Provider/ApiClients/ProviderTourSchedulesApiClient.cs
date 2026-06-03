using YallaJo.Web.Areas.Provider.Models.TourSchedules;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderTourSchedulesApiClient
{
    private readonly IApiClient _api;

    public ProviderTourSchedulesApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/{id}/schedules?activeOnly={activeOnly}
    public Task<ApiResult<List<TourScheduleResponse>>> GetSchedulesAsync(
        Guid tourId, bool activeOnly, CancellationToken ct = default)
        => _api.GetAsync<List<TourScheduleResponse>>(
            $"/api/v1/tours/{tourId}/schedules?activeOnly={(activeOnly ? "true" : "false")}", ct);

    // POST /api/v1/tours/{id}/schedules
    public Task<ApiResult<CreateTourScheduleResponse>> CreateAsync(
        Guid tourId, CreateTourScheduleApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateTourScheduleResponse>($"/api/v1/tours/{tourId}/schedules", request, ct);

    // PUT /api/v1/tours/{id}/schedules/{scheduleId}
    public Task<ApiResult> UpdateAsync(
        Guid tourId, Guid scheduleId, UpdateTourScheduleApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/tours/{tourId}/schedules/{scheduleId}", request, ct);

    // DELETE /api/v1/tours/{id}/schedules/{scheduleId}
    public Task<ApiResult> DeleteAsync(Guid tourId, Guid scheduleId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/tours/{tourId}/schedules/{scheduleId}", ct);
}
