using System.Globalization;
using YallaJo.Web.Areas.Admin.Models.Bookings;
using YallaJo.Web.Areas.Admin.Models.Payments;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

public sealed class PaymentsApiClient
{
    private readonly IApiClient _api;

    public PaymentsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<AdminFinanceDashboardResponse>> GetDashboardAsync(CancellationToken ct = default)
        => _api.GetAsync<AdminFinanceDashboardResponse>("/api/v1/finance/admin/dashboard", ct);

    public Task<ApiResult<PaymentPageResponse>> GetAdminPaymentsAsync(
        string? status,
        string? type,
        Guid? cursor,
        int pageSize,
        CancellationToken ct = default)
    {
        var query = new List<string>
        {
            "pageSize=" + pageSize.ToString(CultureInfo.InvariantCulture),
            "countTotal=true"
        };

        if (!string.IsNullOrWhiteSpace(status))
        {
            query.Add("status=" + Uri.EscapeDataString(status));
        }

        if (!string.IsNullOrWhiteSpace(type))
        {
            query.Add("type=" + Uri.EscapeDataString(type));
        }

        if (cursor is { } c && c != Guid.Empty)
        {
            query.Add("cursor=" + c.ToString("D"));
        }

        var url = "/api/v1/payments/admin/all?" + string.Join("&", query);
        return _api.GetAsync<PaymentPageResponse>(url, ct);
    }

    // POST /api/v1/payments/{id}/refund — refund a completed Booking payment (§8.7).
    // Reuses the shared RefundPaymentRequest contract (Amount, Currency, Reason).
    public Task<ApiResult> RefundAsync(Guid paymentId, RefundPaymentRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/payments/{paymentId}/refund", request, ct);
}
