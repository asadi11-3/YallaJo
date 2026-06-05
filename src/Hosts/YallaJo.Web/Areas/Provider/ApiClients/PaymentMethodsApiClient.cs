using YallaJo.Web.Areas.Provider.Models.PaymentMethods;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class PaymentMethodsApiClient
{
    private const string Base = "/api/v1/provider-payment-methods";

    private readonly IApiClient _api;

    public PaymentMethodsApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<ProviderPaymentMethodResponse>>> GetMethodsAsync(CancellationToken ct = default)
        => _api.GetAsync<List<ProviderPaymentMethodResponse>>(Base, ct);

    public Task<ApiResult<ProviderPaymentMethodResponse>> CreateAsync(ProviderPaymentMethodRequest request, CancellationToken ct = default)
        => _api.PostAsync<ProviderPaymentMethodResponse>(Base, request, ct);

    public Task<ApiResult<ProviderPaymentMethodResponse>> UpdateAsync(Guid id, ProviderPaymentMethodRequest request, CancellationToken ct = default)
        => _api.PutAsync<ProviderPaymentMethodResponse>($"{Base}/{id}", request, ct);

    public Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
        => _api.DeleteAsync($"{Base}/{id}", ct);
}
