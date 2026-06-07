using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Admin.ApiClients;

/// <summary>
/// Admin/KYC actions over provider payout methods. §8.7.
/// Backend currently exposes only the verify mutation to admins; there is no
/// admin-facing list endpoint (GET /provider-payment-methods is scoped to the
/// current user), so this client carries the verify call keyed by method id.
/// </summary>
public sealed class ProviderPaymentMethodsApiClient
{
    private readonly IApiClient _api;

    public ProviderPaymentMethodsApiClient(IApiClient api) => _api = api;

    // POST /api/v1/provider-payment-methods/{id}/verify
    // Body: VerifyProviderPaymentMethodRequest(bool IsVerified). Admin = current user.
    public Task<ApiResult> VerifyAsync(Guid id, bool isVerified, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/provider-payment-methods/{id}/verify", new { IsVerified = isVerified }, ct);
}
