using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Wishlist;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Wishlist;
using YallaJo.Web.Areas.Accounts.ApiClients;
using YallaJo.Web.Areas.Accounts.Models.Wishlist;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Accounts.Facades;

public sealed class WishlistFacade
{
    private readonly WishlistApiClient _api;
    private readonly IApiAssetUrlResolver _assetResolver;

    public WishlistFacade(WishlistApiClient api, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _assetResolver = assetResolver;
    }

    public async Task<ApiResult<WishlistVm>> GetWishlistAsync(CancellationToken ct = default)
    {
        var result = await _api.GetFavoritesAsync(ct);

        if (result.IsUnauthorized)
            return ApiResult<WishlistVm>.ForceSignOut();
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<WishlistVm>.Fail(result.StatusCode, result.Error ?? "Could not load your wishlist.");

        var items = await Task.WhenAll(result.Data.Items.Select(f => BuildItemAsync(f, ct)));

        return ApiResult<WishlistVm>.Ok(new WishlistVm
        {
            Items = items.OrderByDescending(i => i.AddedAt).ToList(),
        });
    }

    private async Task<WishlistItemVm> BuildItemAsync(FavoriteResponse fav, CancellationToken ct)
    {
        // Defaults if hydration fails.
        var title = $"{WishlistMapper.Humanize(fav.EntityType)} item";
        string? subtitle = null;
        string? imageUrl = null;
        decimal? price = null;
        string? currency = null;
        decimal? rating = null;

        try
        {
            switch (fav.EntityType)
            {
                case "Tour":
                {
                    var tour = await _api.GetTourAsync(fav.EntityId, ct);
                    if (tour is { IsSuccess: true, Data: { } t })
                    {
                        title = t.Name;
                        price = t.SalePrice ?? t.BasePrice;
                        currency = t.Currency;
                        rating = t.AverageRating;
                    }
                    imageUrl = await ResolveAttachmentAsync(fav.EntityType, fav.EntityId, ct);
                    break;
                }
                case "Place":
                {
                    var place = await _api.GetPlaceAsync(fav.EntityId, ct);
                    if (place is { IsSuccess: true, Data: { } p })
                    {
                        title = p.Name;
                        subtitle = WishlistMapper.JoinLocation(p.City, p.Country);
                        rating = p.AverageRating;
                    }
                    imageUrl = await ResolveAttachmentAsync(fav.EntityType, fav.EntityId, ct);
                    break;
                }
                case "Business":
                {
                    var biz = await _api.GetBusinessAsync(fav.EntityId, ct);
                    if (biz is { IsSuccess: true, Data: { } b })
                    {
                        title = b.Name;
                        subtitle = WishlistMapper.JoinLocation(b.City, b.Country);
                        rating = b.AverageRating;
                    }
                    imageUrl = await ResolveAttachmentAsync(fav.EntityType, fav.EntityId, ct);
                    break;
                }
                case "TourGuide":
                {
                    var guide = await _api.GetTourGuideAsync(fav.EntityId, ct);
                    if (guide is { IsSuccess: true, Data: { } g })
                    {
                        title = g.DisplayName;
                        subtitle = "Tour guide";
                        rating = g.AverageRating;
                        // TourGuide carries its own avatar; prefer it over attachments.
                        imageUrl = _assetResolver.Resolve(g.AvatarUrl);
                    }
                    imageUrl ??= await ResolveAttachmentAsync(fav.EntityType, fav.EntityId, ct);
                    break;
                }
                case "Blog":
                {
                    var blog = await _api.GetBlogAsync(fav.EntityId, ct);
                    if (blog is { IsSuccess: true, Data: { } bl })
                    {
                        title = bl.Title;
                        subtitle = "Article";
                    }
                    imageUrl = await ResolveAttachmentAsync(fav.EntityType, fav.EntityId, ct);
                    break;
                }
                default:
                    imageUrl = await ResolveAttachmentAsync(fav.EntityType, fav.EntityId, ct);
                    break;
            }
        }
        catch
        {
            // tolerate per-item hydration failure; show the minimal card
        }

        return WishlistMapper.ToVm(
            fav.EntityType, fav.EntityId, title, subtitle, imageUrl, price, currency, rating, fav.AddedAt);
    }

    private async Task<string?> ResolveAttachmentAsync(string entityType, Guid entityId, CancellationToken ct)
    {
        var attach = await _api.GetAttachmentsAsync(entityType, entityId, ct);
        if (attach is { IsSuccess: true, Data: { Count: > 0 } images })
        {
            var primary = images.OrderBy(a => a.SortOrder).First();
            return _assetResolver.Resolve(primary.ThumbnailUrl ?? primary.Url);
        }

        return null;
    }

    public async Task<ApiResult> RemoveAsync(string entityType, Guid entityId, CancellationToken ct = default)
    {
        var result = await _api.RemoveFavoriteAsync(entityType, entityId, ct);

        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        // DELETE is idempotent (204 even when already absent); treat NotFound as success.
        if (result.IsSuccess || result.IsNotFound)
            return ApiResult.Ok();

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not remove the item.");
    }

    public async Task<ApiResult> RemoveAllAsync(CancellationToken ct = default)
    {
        var favorites = await _api.GetFavoritesAsync(ct);

        if (favorites.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (!favorites.IsSuccess || favorites.Data is null)
            return ApiResult.Fail(favorites.StatusCode, favorites.Error ?? "Could not load your wishlist.");

        // No batch endpoint exists; remove each favorite individually.
        var results = await Task.WhenAll(
            favorites.Data.Items.Select(f => _api.RemoveFavoriteAsync(f.EntityType, f.EntityId, ct)));

        if (results.Any(r => r.IsUnauthorized))
            return ApiResult.ForceSignOut();
        if (results.Any(r => !r.IsSuccess && !r.IsNotFound))
            return ApiResult.Fail(500, "Some items could not be removed. Please try again.");

        return ApiResult.Ok();
    }
}
