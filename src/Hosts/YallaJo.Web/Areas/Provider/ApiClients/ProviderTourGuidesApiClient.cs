using YallaJo.Web.Areas.Provider.Models.TourGuides;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Provider.ApiClients;

public sealed class ProviderTourGuidesApiClient
{
    private readonly IApiClient _api;

    public ProviderTourGuidesApiClient(IApiClient api) => _api = api;

    // GET /api/v1/tours/{id}/guides
    public Task<ApiResult<List<TourGuideResponse>>> GetGuidesAsync(Guid tourId, CancellationToken ct = default)
        => _api.GetAsync<List<TourGuideResponse>>($"/api/v1/tours/{tourId}/guides", ct);

    // POST /api/v1/tours/{id}/guides
    public Task<ApiResult> AssignAsync(Guid tourId, AssignTourGuideApiRequest request, CancellationToken ct = default)
        => _api.PostAsync($"/api/v1/tours/{tourId}/guides", request, ct);

    // [Backend] B7 — GET /api/v1/tours/{id}/guides/lookup?term=&pageSize=
    public Task<ApiResult<List<GuideLookupItemResponse>>> LookupAsync(
        Guid tourId, string? term, int pageSize = 10, CancellationToken ct = default)
    {
        var url = $"/api/v1/tours/{tourId}/guides/lookup?pageSize={pageSize}";
        if (!string.IsNullOrWhiteSpace(term)) url += $"&term={Uri.EscapeDataString(term)}";
        return _api.GetAsync<List<GuideLookupItemResponse>>(url, ct);
    }

    // DELETE /api/v1/tours/{id}/guides/{guideUserId}
    public Task<ApiResult> RemoveAsync(Guid tourId, Guid guideUserId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/tours/{tourId}/guides/{guideUserId}", ct);
}
