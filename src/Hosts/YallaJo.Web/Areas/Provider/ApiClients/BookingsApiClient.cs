using YallaJo.Web.Areas.Provider.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class BookingsApiClient
{
    private readonly IApiClient _api;

    public BookingsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<JoinRequestResponse>>> GetJoinRequestsAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<JoinRequestResponse>>("/api/v1/booking/join-requests?myRequestsOnly=false", ct);

    public Task<ApiResult> ApproveJoinRequestAsync(Guid id, ApproveJoinRequestRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/booking/join-requests/{id}/approve", request, ct);

    public Task<ApiResult> RejectJoinRequestAsync(Guid id, RejectJoinRequestRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/booking/join-requests/{id}/reject", request, ct);

    public Task<ApiResult<TourBookingDetailResponse>> GetBookingAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<TourBookingDetailResponse>($"/api/v1/booking/{id}", ct);

    public Task<ApiResult> ConfirmBookingAsync(Guid id, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/booking/{id}/confirm", null, ct);

    public Task<ApiResult> RejectBookingAsync(Guid id, RejectTourBookingRequest request, CancellationToken ct = default) =>
        _api.PostAsync($"/api/v1/booking/{id}/reject", request, ct);
}
