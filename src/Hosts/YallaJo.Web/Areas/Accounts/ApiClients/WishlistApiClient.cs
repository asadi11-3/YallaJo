using YallaJo.Web.Areas.Accounts.Models.Wishlist;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.ApiClients;

public sealed class WishlistApiClient
{
    private readonly IApiClient _api;

    public WishlistApiClient(IApiClient api) => _api = api;

    public Task<ApiResult<FavoritePageResponse>> GetFavoritesAsync(CancellationToken ct = default)
        => _api.GetAsync<FavoritePageResponse>("/api/v1/social/favorites?pageSize=50", ct);

    public Task<ApiResult> RemoveFavoriteAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => _api.DeleteAsync($"/api/v1/social/favorites/{Uri.EscapeDataString(entityType)}/{entityId}", ct);

    public Task<ApiResult<Guid>> AddFavoriteAsync(AddFavoriteRequest request, CancellationToken ct = default)
        => _api.PostAsync<Guid>("/api/v1/social/favorites", request, ct);

    public Task<ApiResult<CheckFavoriteResponse>> CheckFavoriteAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => _api.GetAsync<CheckFavoriteResponse>(
            $"/api/v1/social/favorites/check/{Uri.EscapeDataString(entityType)}/{entityId}", ct);

    public Task<ApiResult<TourLookupResponse>> GetTourAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourLookupResponse>($"/api/v1/tours/{id}", ct);

    public Task<ApiResult<PlaceLookupResponse>> GetPlaceAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<PlaceLookupResponse>($"/api/v1/places/{id}", ct);

    public Task<ApiResult<BusinessLookupResponse>> GetBusinessAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<BusinessLookupResponse>($"/api/v1/places/businesses/{id}", ct);

    public Task<ApiResult<TourGuideLookupResponse>> GetTourGuideAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<TourGuideLookupResponse>($"/api/v1/guides/{id}", ct);

    public Task<ApiResult<BlogLookupResponse>> GetBlogAsync(Guid id, CancellationToken ct = default)
        => _api.GetAsync<BlogLookupResponse>($"/api/v1/blogs/{id}", ct);

    public Task<ApiResult<List<WishlistAttachmentResponse>>> GetAttachmentsAsync(string entityType, Guid entityId, CancellationToken ct = default)
        => _api.GetAsync<List<WishlistAttachmentResponse>>(
            $"/api/v1/content-core/attachments?entityType={Uri.EscapeDataString(entityType)}&entityId={entityId}", ct);
}
