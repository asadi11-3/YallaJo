using Microsoft.AspNetCore.WebUtilities;
using YallaJo.Web.Areas.Accounts.Models.Bookings;
using YallaJo.Web.Areas.Accounts.Models.Payments;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class PaymentsApiClient
{
    private const string Base = "/api/v1/payments";

    private readonly IApiClient _api;

    public PaymentsApiClient(IApiClient api) => _api = api;

    // POST /api/v1/payments/initiate
    public Task<ApiResult<InitiatePaymentResponse>> InitiateAsync(
        Guid bookingId, string returnUrl, CancellationToken ct = default)
        => _api.PostAsync<InitiatePaymentResponse>(
            $"{Base}/initiate",
            // PaymentMethod is fixed to CreditCard for v1 (no card entry; stub gateway).
            new InitiatePaymentApiRequest(bookingId, "CreditCard", returnUrl),
            ct);

    // POST /api/v1/payments/{bookingId}/simulate-success (Development-only on the API)
    public Task<ApiResult<SimulatePaymentResponse>> SimulateSuccessAsync(Guid bookingId, CancellationToken ct = default)
        => _api.PostAsync<SimulatePaymentResponse>($"{Base}/{bookingId}/simulate-success", null, ct);

    // GET /api/v1/payments/my-payments?cursor&pageSize
    public Task<ApiResult<PaymentPageResponse>> GetMyPaymentsAsync(
        Guid? cursor = null, int pageSize = 20, CancellationToken ct = default)
    {
        var query = new Dictionary<string, string?> { ["pageSize"] = pageSize.ToString() };
        if (cursor is { } c)
        {
            query["cursor"] = c.ToString();
        }

        var url = QueryHelpers.AddQueryString($"{Base}/my-payments", query);
        return _api.GetAsync<PaymentPageResponse>(url, ct);
    }
}
