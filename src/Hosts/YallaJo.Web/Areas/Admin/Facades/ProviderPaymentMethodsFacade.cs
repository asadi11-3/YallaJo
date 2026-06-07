using YallaJo.Web.Areas.Admin.ApiClients;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Admin.Facades;

/// <summary>
/// §8.7 — admin/KYC verification of provider payout methods.
/// </summary>
public sealed class ProviderPaymentMethodsFacade
{
    private readonly ProviderPaymentMethodsApiClient _api;

    public ProviderPaymentMethodsFacade(ProviderPaymentMethodsApiClient api) => _api = api;

    public async Task<ApiResult> VerifyAsync(Guid id, bool isVerified, CancellationToken ct = default)
    {
        if (id == Guid.Empty)
        {
            return ApiResult.Fail(400, "A valid payment method is required.");
        }

        var result = await _api.VerifyAsync(id, isVerified, ct);
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
            return ApiResult.Fail(409, result.Error ?? "This payment method can't be updated in its current state.");
        }

        return result.IsSuccess
            ? ApiResult.Ok()
            : ApiResult.Fail(result.StatusCode, result.Error ?? "Could not update the payment method verification.");
    }
}
