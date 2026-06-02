using YallaJo.Web.Areas.Content.Models.Guides;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Content.ApiClients;

public sealed class GuidesApiClient
{
    private readonly IApiClient _api;

    public GuidesApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<ListTourGuidesResponse>> ListGuidesAsync(
        int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<ListTourGuidesResponse>($"/api/v1/guides?page={page}&pageSize={pageSize}", ct);

    public Task<ApiResult<TourGuideProfileResponse>> GetGuideBySlugAsync(
        string slug, CancellationToken ct = default)
        => _api.GetAsync<TourGuideProfileResponse>(
            $"/api/v1/guides/by-slug/{Uri.EscapeDataString(slug)}", ct);

    public Task<ApiResult<GetGuideToursResponse>> ListGuideToursAsync(
        Guid id, int page, int pageSize, CancellationToken ct = default)
        => _api.GetAsync<GetGuideToursResponse>(
            $"/api/v1/guides/{id}/tours?page={page}&pageSize={pageSize}", ct);
}
