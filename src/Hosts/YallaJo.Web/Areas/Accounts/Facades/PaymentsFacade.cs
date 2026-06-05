using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Payments;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class PaymentsFacade
{
    private const int PageSize = 50;

    private readonly PaymentsApiClient _api;

    public PaymentsFacade(PaymentsApiClient api) => _api = api;

    public async Task<ApiResult<PaymentsVm>> GetAsync(CancellationToken ct = default)
    {
        var result = await _api.GetMyPaymentsAsync(pageSize: PageSize, ct: ct);
        if (result.RequireSignOut)
        {
            return ApiResult<PaymentsVm>.ForceSignOut();
        }

        if (!result.IsSuccess || result.Data is null)
        {
            return ApiResult<PaymentsVm>.Fail(result.StatusCode, result.Error);
        }

        var rows = result.Data.Items
            .OrderByDescending(p => p.CreatedAt)
            .Select(ToRow)
            .ToList();

        return ApiResult<PaymentsVm>.Ok(new PaymentsVm { Payments = rows });
    }

    private static PaymentRowVm ToRow(PaymentResponse p) => new(
        p.Id,
        p.BookingId,
        p.Amount,
        p.Currency,
        p.PaymentMethod,
        p.PaymentType,
        p.Status,
        p.RefundedTotal,
        p.PaidAt,
        p.CreatedAt);

    public async Task<ApiResult> PayAsync(Guid bookingId, string returnUrl, CancellationToken ct = default)
    {
        var initiate = await _api.InitiateAsync(bookingId, returnUrl, ct);
        if (initiate.IsUnauthorized) return ApiResult.ForceSignOut();
        if (initiate.IsForbidden) return ApiResult.Fail(403, "You can only pay for your own bookings.");
        if (initiate.IsNotFound) return ApiResult.Fail(404, "Booking not found.");
        if (initiate.IsConflict)
            return ApiResult.Fail(409, initiate.Error ?? "This booking is not awaiting payment.");
        if (initiate.IsValidationError)
            return ApiResult.Fail(initiate.StatusCode, initiate.Error ?? "Payment could not be started yet. Please try again in a moment.");
        if (!initiate.IsSuccess)
            return ApiResult.Fail(initiate.StatusCode, initiate.Error ?? "Could not start payment.");

        var simulate = await _api.SimulateSuccessAsync(bookingId, ct);
        if (simulate.IsUnauthorized) return ApiResult.ForceSignOut();
        if (simulate.IsSuccess || simulate.IsNotFound)
            return ApiResult.Ok();

        if (simulate.IsForbidden) return ApiResult.Fail(403, "You can only pay for your own bookings.");
        if (simulate.IsConflict)
            return ApiResult.Fail(409, simulate.Error ?? "Payment is still being set up. Please try again in a moment.");

        return ApiResult.Fail(simulate.StatusCode, simulate.Error ?? "Could not complete payment.");
    }
}
