using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class BookingsApiClient
{
    private readonly IApiClient _api;

    public BookingsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<MyBookingsPageResponse>> GetMyBookingsAsync(IEnumerable<string> statuses, CancellationToken ct = default)
    {
        var status = string.Join(',', statuses);
        var path = $"/api/v1/booking/my-bookings?pageSize=50&status={Uri.EscapeDataString(status)}";
        return _api.GetAsync<MyBookingsPageResponse>(path, ct);
    }

    public Task<ApiResult<TourBookingDetailResponse>> GetBookingAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourBookingDetailResponse>($"/api/v1/booking/{id}", ct);

    public Task<ApiResult> CancelAsync(Guid id, CancelBookingRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/booking/{id}/cancel", request, ct);

    // POST /api/v1/booking/{id}/dispute — owner opens a dispute on a Completed booking (FE-1A)
    public Task<ApiResult> OpenDisputeAsync(Guid id, OpenBookingDisputeRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/booking/{id}/dispute", request, ct);

    public Task<ApiResult<TourLookupResponse>> GetTourAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourLookupResponse>($"/api/v1/tours/{id}", ct);

    public Task<ApiResult<List<AttachmentResponse>>> GetAttachmentsAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => _api.GetAsync<List<AttachmentResponse>>(
            $"/api/v1/content-core/attachments?entityType={Uri.EscapeDataString(entityType)}&entityId={entityId}", ct);
}
