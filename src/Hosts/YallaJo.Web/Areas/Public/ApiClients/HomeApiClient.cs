using YallaJo.Web.Areas.Public.Models.Home;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.ApiClients;

/// <summary>Talks to the public read endpoints that feed the homepage.</summary>
public sealed class HomeApiClient
{
    private readonly IApiClient _api;

    public HomeApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<List<FeaturedTourResponse>>> GetFeaturedToursAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<FeaturedTourResponse>>("/api/v1/tours/featured", ct);

    public Task<ApiResult<List<PopularEntityResponse>>> GetPopularToursAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<PopularEntityResponse>>("/api/v1/popular/tours", ct);

    public Task<ApiResult<List<PopularEntityResponse>>> GetTrendingAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<PopularEntityResponse>>("/api/v1/trending", ct);

    public Task<ApiResult<List<HomeCategoryResponse>>> GetCategoriesAsync(CancellationToken ct = default) =>
        _api.GetAsync<List<HomeCategoryResponse>>("/api/v1/content-core/categories", ct);

    public Task<ApiResult<TourDetailResponse>> GetTourByIdAsync(Guid id, CancellationToken ct = default) =>
        _api.GetAsync<TourDetailResponse>($"/api/v1/tours/{id}", ct);
}
