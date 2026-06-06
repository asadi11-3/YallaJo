using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
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
        var placesTask = BuildPopularPlacesAsync(ct);
        var businessesTask = BuildPopularBusinessesAsync(ct);
        var categoriesTask = BuildCategoriesAsync(ct);

        await Task.WhenAll(featuredTask, popularTask, placesTask, businessesTask, categoriesTask);

        var vm = new HomeVm
        {
            FeaturedTours = featuredTask.Result,
            PopularTours = popularTask.Result,
            PopularPlaces = placesTask.Result,
            PopularBusinesses = businessesTask.Result,
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

    private async Task<IReadOnlyList<HomePlaceCardVm>> BuildPopularPlacesAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPopularPlacesAsync(ct);
            if (result is not { IsSuccess: true, Data: { Count: > 0 } items })
                return [];

            var placeIds = items
                .Where(p => string.Equals(p.EntityType, "Place", StringComparison.OrdinalIgnoreCase))
                .Take(PopularLimit)
                .Select(p => p.EntityId)
                .ToList();

            // The popular/places feed is already place-scoped; fall back to raw IDs if it omits EntityType.
            if (placeIds.Count == 0)
                placeIds = items.Take(PopularLimit).Select(p => p.EntityId).ToList();

            var cards = await Task.WhenAll(placeIds.Select(id => BuildPlaceCardAsync(id, ct)));
            return cards.Where(c => c is not null).Select(c => c!).ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<HomePlaceCardVm?> BuildPlaceCardAsync(Guid placeId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPlaceByIdAsync(placeId, ct);
            // Drop places we cannot deep-link (slug missing) so the rail never renders broken links.
            if (result is not { IsSuccess: true, Data: { } p } || string.IsNullOrWhiteSpace(p.Slug))
                return null;

            return new HomePlaceCardVm
            {
                Id = p.Id,
                Name = p.Name,
                Slug = p.Slug,
                ImageUrl = null,
                City = p.City,
                Country = p.Country,
                AverageRating = p.AverageRating,
                ReviewCount = p.ReviewCount,
                IsFeatured = p.IsFeatured,
            };
        }
        catch
        {
            return null;
        }
    }

    private async Task<IReadOnlyList<HomeBusinessCardVm>> BuildPopularBusinessesAsync(CancellationToken ct)
    {
        try
        {
            var result = await _api.GetPopularBusinessesAsync(ct);
            if (result is not { IsSuccess: true, Data: { Count: > 0 } items })
                return [];

            var businessIds = items
                .Where(p => string.Equals(p.EntityType, "Business", StringComparison.OrdinalIgnoreCase))
                .Take(PopularLimit)
                .Select(p => p.EntityId)
                .ToList();

            // The popular/businesses feed is already business-scoped; fall back to raw IDs if it omits EntityType.
            if (businessIds.Count == 0)
                businessIds = items.Take(PopularLimit).Select(p => p.EntityId).ToList();

            var cards = await Task.WhenAll(businessIds.Select(id => BuildBusinessCardAsync(id, ct)));
            return cards.Where(c => c is not null).Select(c => c!).ToList();
        }
        catch
        {
            return [];
        }
    }

    private async Task<HomeBusinessCardVm?> BuildBusinessCardAsync(Guid businessId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetBusinessByIdAsync(businessId, ct);
            if (result is not { IsSuccess: true, Data: { } b } || string.IsNullOrWhiteSpace(b.Slug))
                return null;

            return new HomeBusinessCardVm
            {
                Id = b.Id,
                Name = b.Name,
                Slug = b.Slug,
                ImageUrl = null,
                BusinessType = b.BusinessType,
                City = b.City,
                Country = b.Country,
                AverageRating = b.AverageRating,
                ReviewCount = b.ReviewCount,
                IsFeatured = b.IsFeatured,
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

    // Featured/popular tour cards expose no public image field and the attachment
    // endpoint is not anonymous-accessible, so cards use a deterministic theme
    // placeholder (temporary public image API gap — see PublicImagePlaceholder).
    private static Task<string?> ResolveCoverAsync(Guid tourId, CancellationToken ct)
        => Task.FromResult<string?>(PublicImagePlaceholder.ResolveTourImage(tourId));
}
