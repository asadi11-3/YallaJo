using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class BookingsFacade
{
    private const int DefaultPageSize = 25;

    private readonly BookingsApiClient _api;

    public BookingsFacade(BookingsApiClient api) => _api = api;

    public async Task<ApiResult<BookingsVm>> GetIndexAsync(BookingsFilterRequest request, CancellationToken ct = default)
    {
        var pageSize = request.PageSize is < 1 or > 100 ? DefaultPageSize : request.PageSize;

        var result = await _api.GetAdminBookingsAsync(
            request.Status,
            request.FromDate,
            request.ToDate,
            request.UserId,
            request.ProviderId,
            request.TourId,
            request.Cursor,
            pageSize,
            ct);

        if (result.IsUnauthorized)
        {
            return ApiResult<BookingsVm>.ForceSignOut();
        }

        if (result is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<BookingsVm>.Fail(result.StatusCode, result.Error ?? "Could not load bookings.");
        }

        var vm = BookingsMapper.ToVm(result.Data, request.Status, request.FromDate, request.ToDate);
        return ApiResult<BookingsVm>.Ok(vm);
    }

    public Task<ApiResult> ForceRefundAsync(Guid id, string reason, CancellationToken ct = default)
        => Normalize(_api.ForceRefundAsync(id, reason, ct), "Could not force-refund the booking.");

    private static async Task<ApiResult> Normalize(Task<ApiResult> call, string fallback)
    {
        var result = await call;

        if (result.IsSuccess)
        {
            return ApiResult.Ok();
        }

        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsNotFound)
        {
            return ApiResult.Fail(404, "Booking not found.");
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, "This booking can no longer be force-refunded in its current state. Please reload and try again.");
        }

        if (result.IsValidationError)
        {
            return ApiResult.Invalid(result.ValidationErrors!);
        }

        return ApiResult.Fail(result.StatusCode, result.Error ?? fallback);
    }
}
