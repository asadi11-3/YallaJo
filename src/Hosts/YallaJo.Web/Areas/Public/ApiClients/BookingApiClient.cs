using YallaJo.Web.Areas.Public.Models.Booking;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

public sealed class BookingApiClient
{
    private readonly IApiClient _api;

    public BookingApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<TourDetailResponse>> GetTourAsync(Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<TourDetailResponse>($"/api/v1/tours/{tourId}", ct);

    public Task<ApiResult<AvailabilityPageResponse>> GetAvailabilityAsync(Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<AvailabilityPageResponse>($"/api/v1/booking/availability/{tourId}?pageSize=20", ct);

    public Task<ApiResult<List<TourAttachmentResponse>>> GetAttachmentsAsync(Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<List<TourAttachmentResponse>>($"/api/v1/content-core/attachments?entityType=Tour&entityId={tourId}", ct);

    public Task<ApiResult<CreateTourBookingResponse>> CreateBookingAsync(CreateTourBookingRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateTourBookingResponse>("/api/v1/booking/tour", request, ct);

    public Task<ApiResult<TourBookingDetailResponse>> GetBookingAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourBookingDetailResponse>($"/api/v1/booking/{id}", ct);
}
