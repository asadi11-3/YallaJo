using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Areas.Admin.Models.Payments;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

public sealed class PaymentsFacade
{
    private readonly PaymentsApiClient _api;

    public PaymentsFacade(PaymentsApiClient api) => _api = api;

    public async Task<ApiResult<PaymentsVm>> GetEarningsAsync(PaymentsFilterRequest request, CancellationToken ct = default)
    {
        // UI-UX-D1/R4: cap the page size at 50 (was 200).
        var pageSize = Math.Clamp(request.PageSize, 1, 50);

        var dashboard = await _api.GetDashboardAsync(ct);
        if (dashboard.IsUnauthorized)
        {
            return ApiResult<PaymentsVm>.ForceSignOut();
        }

        if (dashboard is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<PaymentsVm>.Fail(dashboard.StatusCode, dashboard.Error ?? "Could not load the finance dashboard.");
        }

        var payments = await _api.GetAdminPaymentsAsync(request.Status, request.Type, request.Cursor, pageSize, ct);
        if (payments.IsUnauthorized)
        {
            return ApiResult<PaymentsVm>.ForceSignOut();
        }

        if (payments is not { IsSuccess: true, Data: not null })
        {
            return ApiResult<PaymentsVm>.Fail(payments.StatusCode, payments.Error ?? "Could not load payments.");
        }

        var vm = PaymentsMapper.ToVm(dashboard.Data, payments.Data, request.Status, request.Type);
        return ApiResult<PaymentsVm>.Ok(vm);
    }

    // §8.7 — refund a completed Booking payment from the Finance page.
    public async Task<ApiResult> RefundAsync(Guid paymentId, decimal amount, string? currency, string? reason, CancellationToken ct = default)
    {
        if (paymentId == Guid.Empty)
        {
            return ApiResult.Fail(400, "A valid payment is required.");
        }

        if (amount <= 0m)
        {
            return ApiResult.Fail(400, "Refund amount must be greater than zero.");
        }

        var normalizedCurrency = string.IsNullOrWhiteSpace(currency)
            ? "JOD"
            : currency.Trim().ToUpperInvariant();

        var request = new RefundPaymentRequest(amount, normalizedCurrency, (reason ?? string.Empty).Trim());

        var result = await _api.RefundAsync(paymentId, request, ct);
        if (result.IsUnauthorized)
        {
            return ApiResult.ForceSignOut();
        }

        if (result.IsValidationError && result.ValidationErrors is not null)
        {
            return ApiResult.Invalid(result.ValidationErrors);
        }

        if (result.IsConflict)
        {
            return ApiResult.Fail(409, result.Error ?? "This payment can't be refunded in its current state.");
        }

        return result.IsSuccess
            ? ApiResult.Ok()
            : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not process the refund.");
    }
}
