using YallaJo.Web.Areas.Business.Models.Accessibility;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Business.ApiClients;

public sealed class AccessibilityApiClient
{
    private const string Base = "/api/v1/places/businesses";
    private readonly IApiClient _api;

    public AccessibilityApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<AccessibilityFeatureItemResponse>>> GetAsync(Guid businessId, CancellationToken ct = default) =>
        _api.GetAsync<List<AccessibilityFeatureItemResponse>>($"{Base}/{businessId:D}/accessibility", ct);

    public Task<ApiResult> SaveAsync(Guid businessId, IReadOnlyList<AccessibilityFeatureItemApiRequest> features, CancellationToken ct = default) =>
        _api.PutAsync($"{Base}/{businessId:D}/accessibility", features, ct);
}
