using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Models.Home;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

/// <summary>Assembles the homepage view model from several public read endpoints.</summary>
public sealed class HomeFacade
{
    private const int PopularLimit = 6;

    private readonly HomeApiClient _api;
    private readonly IApiAssetUrlResolver _assetResolver;

    public HomeFacade(HomeApiClient api, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _assetResolver = assetResolver;
    }

    /// <summary>
    /// Best-effort homepage build: any rail that fails to load is simply left empty
    /// rather than failing the whole page.
    /// </summary>
    public async Task<ApiResult<HomeVm>> GetHomeAsync(CancellationToken ct = default)
    {
        var featuredTask = BuildFeaturedAsync(ct);
        var popularTask = BuildPopularAsync(ct);
        var categoriesTask = BuildCategoriesAsync(ct);

        await Task.WhenAll(featuredTask, popularTask, categoriesTask);

        var vm = new HomeVm
        {
            FeaturedTours = featuredTask.Result,
            PopularTours = popularTask.Result,
            Categories = categoriesTask.Result,
        };

        return ApiResult<HomeVm>.Ok(vm);
    }

    private async Task<IReadOnlyList<HomeTourCardVm>> BuildFeaturedAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetFeaturedToursAsync(ct);
            if (result is not { IsSuccess: true, Data: { Count: > 0 } items })
                return [];

            var cards = await Task.WhenAll(items.Select(t => BuildFeaturedCardAsync(t, ct)));
            return cards;
        }
        catch
        {
            return [];
        }
    }

    private async Task<HomeTourCardVm> BuildFeaturedCardAsync(FeaturedTourResponse t, CancellationToken ct)
    {
        return new HomeTourCardVm
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            ImageUrl = await ResolveCoverAsync(t.Id, ct),
            BasePrice = t.BasePrice,
            SalePrice = t.SalePrice,
            Currency = string.IsNullOrWhiteSpace(t.Currency) ? "USD" : t.Currency,
            AverageRating = t.AverageRating,
            ReviewCount = t.ReviewCount,
            BookingCount = t.BookingCount,
            IsFeatured = t.IsFeatured,
        };
    }

    private async Task<IReadOnlyList<HomeTourCardVm>> BuildPopularAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPopularToursAsync(ct);
            if (result is not { IsSuccess: true, Data: { Count: > 0 } items })
                return [];

            var tourIds = items
                .Where(p => string.Equals(p.EntityType, "Tour", StringComparison.OrdinalIgnoreCase))
                .Take(PopularLimit)
                .Select(p => p.EntityId)
                .ToList();

            var cards = await Task.WhenAll(tourIds.Select(id => BuildPopularCardAsync(id, ct)));
            return cards.Where(c => c is not null).Select(c => c!).ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<HomeTourCardVm?> BuildPopularCardAsync(Guid tourId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetTourByIdAsync(tourId, ct);
            if (result is not { IsSuccess: true, Data: { } t })
                return null;

            return new HomeTourCardVm
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                ImageUrl = await ResolveCoverAsync(t.Id, ct),
                BasePrice = t.BasePrice,
                SalePrice = t.SalePrice,
                Currency = string.IsNullOrWhiteSpace(t.Currency) ? "USD" : t.Currency,
                AverageRating = (decimal)t.AverageRating,
                ReviewCount = t.ReviewCount,
                BookingCount = t.BookingCount,
                IsFeatured = t.IsFeatured,
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<HomeCategoryVm>> BuildCategoriesAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetCategoriesAsync(ct);
            if (result is not { IsSuccess: true, Data: { Count: > 0 } items })
                return [];

            return items
                .Where(c => c.IsActive)
                .OrderBy(c => c.SortOrder)
                .Select(c => new HomeCategoryVm
                {
                    Id = c.Id,
                    Name = c.Name,
                    Slug = c.Slug,
                    Icon = c.Icon,
                })
                .ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<string?> ResolveCoverAsync(Guid tourId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetAttachmentsAsync(tourId, ct);
            if (result is not { IsSuccess: true, Data: { Count: > 0 } images })
                return null;

            var primary = images.OrderBy(a => a.SortOrder).First();
            return _assetResolver.Resolve(primary.ThumbnailUrl ?? primary.Url);
        }
        catch
        {
            return null;
        }
    }
}
