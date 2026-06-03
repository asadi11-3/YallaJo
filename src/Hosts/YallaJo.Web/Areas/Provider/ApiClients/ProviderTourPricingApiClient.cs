using YallaJo.Web.Areas.Provider.Models.TourPricing;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderTourPricingApiClient
{
    private readonly IApiClient _api;

    public ProviderTourPricingApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/{id}/pricing?activeOnly={activeOnly}
    public Task<ApiResult<List<TourPricingTierResponse>>> GetPricingAsync(
        Guid tourId, bool activeOnly, CancellationToken ct = default)
        => _api.GetAsync<List<TourPricingTierResponse>>(
            $"/api/v1/tours/{tourId}/pricing?activeOnly={(activeOnly ? "true" : "false")}", ct);

    // POST /api/v1/tours/{id}/pricing
    public Task<ApiResult<CreateTourPricingTierResponse>> CreateAsync(
        Guid tourId, CreateTourPricingTierApiRequest request, CancellationToken ct = default)
        => _api.PostAsync<CreateTourPricingTierResponse>($"/api/v1/tours/{tourId}/pricing", request, ct);

    // PUT /api/v1/tours/{id}/pricing/{tierId}
    public Task<ApiResult> UpdateAsync(
        Guid tourId, Guid tierId, UpdateTourPricingTierApiRequest request, CancellationToken ct = default)
        => _api.PutAsync($"/api/v1/tours/{tourId}/pricing/{tierId}", request, ct);

    // DELETE /api/v1/tours/{id}/pricing/{tierId}
    public Task<ApiResult> DeleteAsync(Guid tourId, Guid tierId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/tours/{tourId}/pricing/{tierId}", ct);
}
