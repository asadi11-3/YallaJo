using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class BookingsFacade
{
    private readonly BookingsApiClient _api;

    public BookingsFacade(BookingsApiClient api) => _api = api;

    public async Task<ApiResult<BookingsVm>> GetAsync(Guid? lookupId, CancellationToken ct = default)
    {
        var queue = await GetJoinRequestRowsAsync(ct);
        if (queue.IsUnauthorized)
            return ApiResult<BookingsVm>.ForceSignOut();

        BookingDetailVm? lookupResult = null;
        string? lookupError = null;

        if (lookupId is { } id)
        {
            var detail = await _api.GetBookingAsync(id, ct);
            if (detail.IsUnauthorized)
                return ApiResult<BookingsVm>.ForceSignOut();

            if (detail is { IsSuccess: true, Data: not null })
                lookupResult = MapDetail(detail.Data);
            else if (detail.IsNotFound)
                lookupError = "No booking was found for that id.";
            else if (detail.IsForbidden)
                lookupError = "You do not have access to that booking.";
            else
                lookupError = detail.Error ?? "Could not load the booking.";
        }

        var vm = new BookingsVm
        {
            JoinRequests = queue.Data ?? [],
            LookupResult = lookupResult,
            LookupId = lookupId,
            LookupError = lookupError,
        };

        return ApiResult<BookingsVm>.Ok(vm);
    }

    public async Task<ApiResult> ApproveJoinAsync(Guid id, string? responseMessage, CancellationToken ct = default)
    {
        var result = await _api.ApproveJoinRequestAsync(id, new ApproveJoinRequestRequest(Trim(responseMessage)), ct);
        return Normalize(result, "Could not approve the join request.");
    }

    public async Task<ApiResult> RejectJoinAsync(Guid id, string? responseMessage, CancellationToken ct = default)
    {
        var result = await _api.RejectJoinRequestAsync(id, new RejectJoinRequestRequest(Trim(responseMessage)), ct);
        return Normalize(result, "Could not decline the join request.");
    }

    public async Task<ApiResult> ConfirmBookingAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.ConfirmBookingAsync(id, ct);
        return Normalize(result, "Could not confirm the booking.");
    }

    public async Task<ApiResult> RejectBookingAsync(Guid id, string reason, CancellationToken ct = default)
    {
        var result = await _api.RejectBookingAsync(id, new RejectTourBookingRequest(reason.Trim()), ct);
        return Normalize(result, "Could not reject the booking.");
    }

    private async Task<ApiResult<List<JoinRequestRowVm>>> GetJoinRequestRowsAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetJoinRequestsAsync(ct);
            if (result.IsUnauthorized)
                return ApiResult<List<JoinRequestRowVm>>.ForceSignOut();

            if (result is not { IsSuccess: true, Data: not null })
                return ApiResult<List<JoinRequestRowVm>>.Ok([]);

            var rows = result.Data
                .OrderByDescending(r => r.CreatedAt)
                .Select(r => new JoinRequestRowVm
                {
                    Id = r.Id,
                    Status = r.Status,
                    ParticipantCount = r.ParticipantCount,
                    Message = r.Message,
                    CreatedAt = r.CreatedAt,
                    ExpiresAt = r.ExpiresAt,
                    ResponseMessage = r.ResponseMessage,
                })
                .ToList();

            return ApiResult<List<JoinRequestRowVm>>.Ok(rows);
        }
        catch
        {
            return ApiResult<List<JoinRequestRowVm>>.Ok([]);
        }
    }

    private static BookingDetailVm MapDetail(TourBookingDetailResponse d)
    {
        var pricing = d.Pricing;
        return new BookingDetailVm
        {
            Id = d.Id,
            Reference = d.Reference,
            Status = d.Status,
            ParticipantCount = d.ParticipantCount,
            IsInstantBooking = d.IsInstantBooking,
            SpecialRequests = d.SpecialRequests,
            CreatedAt = d.CreatedAt,
            Subtotal = pricing?.Subtotal ?? 0m,
            DiscountAmount = pricing?.DiscountAmount ?? 0m,
            LoyaltyAmount = pricing?.LoyaltyAmount ?? 0m,
            TotalAmount = pricing?.TotalAmount ?? 0m,
            Currency = pricing?.Currency ?? string.Empty,
            LineItems = (pricing?.LineItems ?? [])
                .Select(li => new BookingLineVm
                {
                    TierType = li.TierType,
                    Count = li.Count,
                    UnitPrice = li.UnitPrice,
                })
                .ToList(),
            CancelledReason = d.Cancellation?.Reason,
            CancelledAt = d.Cancellation?.CancelledAt,
            RejectionReason = d.Rejection?.Reason,
        };
    }

    private static ApiResult Normalize(ApiResult result, string fallback)
    {
        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsSuccess)
            return ApiResult.Ok(result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);
        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }

    private static string? Trim(string? value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
