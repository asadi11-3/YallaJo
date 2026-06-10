using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Directory;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Areas.Public.Models.Shared;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Areas.Public.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Seo;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class PlacesFacade
{
    private const int PageSize = 20;

    // Small, fixed number of related tours shown on the place detail page.
    private const int RelatedToursCount = 4;

    private readonly PlacesApiClient _api;
    private readonly ToursApiClient _toursApi;
    private readonly SeoApiClient _seo;
    private readonly TranslationsApiClient _translations;
    private readonly IApiAssetUrlResolver _assetResolver;

    public PlacesFacade(
        PlacesApiClient api,
        ToursApiClient toursApi,
        SeoApiClient seo,
        TranslationsApiClient translations,
        IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _toursApi = toursApi;
        _seo = seo;
        _translations = translations;
        _assetResolver = assetResolver;
    }

    public async Task<ApiResult<PlacesGridVm>> GetGridAsync(
        PlaceFiltersVm filters, int page, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;

        var result = await _api.ListAsync(
            pageNumber,
            PageSize,
            filters.City,
            filters.Country,
            filters.EffectiveRatingMin,
            filters.HasActiveTours,
            ct);

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<PlacesGridVm>.Fail(result.StatusCode, result.Error ?? "Could not load places.");

        var data = result.Data;
        var cards = data.Items
            .Select(p => new PlaceCardVm
            {
                Id            = p.Id,
                Name          = p.Name,
                Slug          = p.Slug,
                ImageUrl      = PublicImagePlaceholder.ResolvePlaceImage(p.Id),
                PlaceType     = p.PlaceType,
                City          = p.City,
                Country       = p.Country,
                AverageRating = p.AverageRating,
                ReviewCount   = p.ReviewCount,
                IsFeatured    = p.IsFeatured,
                IsVerified    = p.IsVerified,
            })
            .ToList();

        return ApiResult<PlacesGridVm>.Ok(new PlacesGridVm
        {
            Places          = cards,
            PageNumber      = data.PageNumber,
            PageSize        = data.PageSize,
            TotalCount      = data.TotalCount,
            TotalPages      = data.TotalPages,
            HasPreviousPage = data.HasPreviousPage,
            HasNextPage     = data.HasNextPage,
            Filters         = filters,
        });
    }

    public async Task<ApiResult<PlaceDetailVm>> GetDetailAsync(string slug, CancellationToken ct = default)
    {
        var result = await _api.GetBySlugAsync(slug, ct);
        if (!result.IsSuccess || result.Data is null)
            return ApiResult<PlaceDetailVm>.Fail(result.StatusCode, result.Error ?? "Place not found.");

        var d = result.Data;
        var languageCode = TranslationOverlay.ActiveLanguageCode;
        var translationsTask = TranslationOverlay.GetApprovedAsync(_translations, "Place", d.Id, languageCode, ct);
        var translations = await translationsTask;
        var vm = new PlaceDetailVm
        {
            Id                     = d.Id,
            PlaceId                = d.Id,
            Name                   = TranslationOverlay.Apply(translations, "Name", d.Name, languageCode) ?? d.Name,
            Slug                   = d.Slug,
            ImageUrl               = PublicImagePlaceholder.ResolvePlaceImage(d.Id),
            PlaceType              = d.PlaceType,
            Latitude               = d.Latitude,
            Longitude              = d.Longitude,
            Description            = TranslationOverlay.Apply(translations, "Description", d.Description, languageCode),
            Address                = TranslationOverlay.Apply(translations, "Address", d.Address, languageCode),
            City                   = TranslationOverlay.Apply(translations, "City", d.City, languageCode),
            Country                = TranslationOverlay.Apply(translations, "Country", d.Country, languageCode),
            PostalCode             = d.PostalCode,
            Phone                  = d.Phone,
            Email                  = d.Email,
            Website                = d.Website,
            AverageRating          = d.AverageRating,
            ReviewCount            = d.ReviewCount,
            IsFeatured             = d.IsFeatured,
            IsVerified             = d.IsVerified,
            IsWheelchairAccessible = d.IsWheelchairAccessible,
            HasAudioGuide          = d.HasAudioGuide,
            HasBrailleSignage      = d.HasBrailleSignage,
            MetaTitle              = TranslationOverlay.Apply(translations, "MetaTitle", d.MetaTitle, languageCode),
        };

        // CP-3c: hydrate a small set of related (approved) tours for this place.
        // Tolerant — a failed/empty fetch leaves RelatedTours empty and never
        // breaks the place detail page.
        vm.RelatedTours = await GetRelatedToursAsync(d.Id, ct);

        // CP-4: real uploaded place images (tolerant; empty → placeholder used).
        vm.ImageUrls = await GetImageUrlsAsync(d.Id, ct);

        // SEO metadata + FAQ (best-effort; never breaks the page).
        vm.Seo = TranslationOverlay.ApplySeo(
            await GetSeoAsync(d, ct), translations, languageCode,
            fallbackTitle: vm.MetaTitle,
            fallbackDescription: vm.Description);

        await HydrateDetailRailsAsync(vm, ct);

        return ApiResult<PlaceDetailVm>.Ok(vm);
    }

    public Task<ApiResult> RecordSponsoredClickAsync(SponsoredClickFormVm vm, CancellationToken ct = default)
    {
        var body = new SponsoredClickBody(vm.BidId, vm.SourceKind, vm.SourceId, vm.Position, vm.DwellTimeSeconds);
        return _api.RecordSponsoredClickAsync(body, ct);
    }

    private async Task HydrateDetailRailsAsync(PlaceDetailVm vm, CancellationToken ct)
    {
        try
        {
            var accessibilityTask = SafeListAsync(() => _api.GetAccessibilityAsync(vm.PlaceId, ct));
            var catalogTask = SafeListAsync(() => _api.GetAccessibilityCatalogAsync(ct));
            var businessesTask = _api.GetBusinessesAsync(vm.PlaceId, 1, 6, ct);
            var weatherTask = GetWeatherAsync(vm.PlaceId, ct);
            var recommendationsTask = GetRecommendationsAsync(vm.PlaceId, ct);
            var similarTask = GetSimilarRecommendationsAsync(vm.PlaceId, ct);

            await Task.WhenAll(accessibilityTask, catalogTask, businessesTask, weatherTask, recommendationsTask, similarTask);

            vm.AccessibilityFeatures = accessibilityTask.Result
                .Select(a => new PlaceAccessibilityFeatureVm
                {
                    FeatureType = a.FeatureType,
                    Name = a.Name,
                    Description = a.Description,
                    IsAvailable = a.IsAvailable,
                })
                .ToList();

            vm.AccessibilityCatalog = catalogTask.Result
                .Select(c => new PlaceAccessibilityCatalogItemVm { Code = c.Code, DisplayName = c.DisplayName })
                .ToList();

            if (businessesTask.Result is { IsSuccess: true, Data: { } businesses })
            {
                vm.Businesses = businesses.Items.Select(BuildBusinessCard).ToList();
            }

            vm.Weather = weatherTask.Result;
            vm.Recommendations = recommendationsTask.Result;
            vm.SimilarRecommendations = similarTask.Result;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Place detail rails are best-effort; the primary place content still renders.
        }
    }

    private async Task<WeatherResponse?> GetWeatherAsync(Guid placeId, CancellationToken ct)
    {
        try
        {
            var result = await _seo.GetWeatherAsync(placeId, ct);
            if (result is { IsSuccess: true, Data: { } weather }) return weather;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Weather is best-effort.
        }

        return null;
    }

    private async Task<IReadOnlyList<PlaceRecommendationVm>> GetRecommendationsAsync(Guid placeId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetRecommendationsForAsync("Place", placeId, 4, ct);
            if (result is { IsSuccess: true, Data: { } data }) return data.Items.Select(ToRecommendationVm).ToList();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Recommendations are best-effort.
        }

        return [];
    }

    private async Task<IReadOnlyList<PlaceRecommendationVm>> GetSimilarRecommendationsAsync(Guid placeId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetSimilarRecommendationsAsync(placeId, "Place", 4, ct);
            if (result is { IsSuccess: true, Data: { } data }) return data.Items.Select(ToRecommendationVm).ToList();
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // Recommendations are best-effort.
        }

        return [];
    }

    /// <summary>
    /// Loads SEO metadata + FAQ for the place detail page via the shared SEO API.
    /// Best-effort: falls back to the place's own MetaTitle/MetaDescription and a
    /// placeholder image; only cancellation propagates.
    /// </summary>
    private async Task<SeoContent> GetSeoAsync(PlaceDetailResponse d, CancellationToken ct)
    {
        var metaTitle = d.MetaTitle;
        var metaDescription = d.MetaDescription;
        string? canonical = null;
        var ogImage = PublicImagePlaceholder.ResolvePlaceImage(d.Id);
        IReadOnlyList<SeoFaqItem> faqs = [];

        try
        {
            var metaTask = _seo.GetMetadataAsync(SeoEntityType.Place, d.Id, ct);
            var faqTask = _seo.GetFaqAsync(SeoEntityType.Place, d.Id, ct);
            await Task.WhenAll(metaTask, faqTask);

            if (metaTask.Result is { IsSuccess: true, Data: { } meta })
            {
                if (!string.IsNullOrWhiteSpace(meta.Title)) metaTitle = meta.Title;
                if (!string.IsNullOrWhiteSpace(meta.Description)) metaDescription = meta.Description;
                canonical = meta.Canonical;
                if (!string.IsNullOrWhiteSpace(meta.OgImage)) ogImage = meta.OgImage;
            }

            if (faqTask.Result is { IsSuccess: true, Data: { } items })
            {
                faqs = items
                    .OrderBy(f => f.SortOrder)
                    .Select(f => new SeoFaqItem { Question = f.Question, Answer = f.Answer })
                    .ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // tolerate SEO hydration failure — fall back to the place's own meta fields
        }

        return new SeoContent
        {
            MetaTitle = metaTitle,
            MetaDescription = metaDescription,
            Canonical = canonical,
            OgImage = ogImage,
            Faqs = faqs,
        };
    }

    private async Task<IReadOnlyList<string>> GetImageUrlsAsync(Guid placeId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetImagesAsync(placeId, ct);
            if (result is { IsSuccess: true, Data: { } images })
            {
                return images
                    .Select(i => _assetResolver.Resolve(i.Url))
                    .Where(u => !string.IsNullOrWhiteSpace(u))
                    .Select(u => u!)
                    .ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // tolerate image hydration failure
        }

        return [];
    }

    /// <summary>
    /// Loads up to <see cref="RelatedToursCount"/> approved public tours linked
    /// to the place via <c>GET /api/v1/tours?placeId=...</c>. Never throws to the
    /// caller (except cancellation) — returns an empty list on any failure.
    /// </summary>
    private async Task<IReadOnlyList<TourCardVm>> GetRelatedToursAsync(Guid placeId, CancellationToken ct)
    {
        try
        {
            var result = await _toursApi.GetToursAsync(
                page: 1, pageSize: RelatedToursCount, sort: "popularity_desc", placeId: placeId, ct: ct);

            if (result is { IsSuccess: true, Data: { } data })
            {
                return data.Items
                    .Select(t => TourGridMapper.ToCardVm(t, PublicImagePlaceholder.ResolveTourImage(t.Id)))
                    .ToList();
            }
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // tolerate related-tours hydration failure
        }

        return [];
    }

    private BusinessCardVm BuildBusinessCard(BusinessSummaryResponse b)
    {
        var imageUrl = !string.IsNullOrWhiteSpace(b.PrimaryImageUrl)
            ? _assetResolver.Resolve(b.PrimaryImageUrl)
            : PublicImagePlaceholder.ResolveBusinessImage(b.Id);

        return new BusinessCardVm
        {
            Id = b.Id,
            Name = b.Name,
            Slug = b.Slug,
            ImageUrl = imageUrl,
            BusinessType = b.BusinessType,
            Location = PlaceCardVm.FormatLocation(b.City, b.Country),
            AverageRating = b.AverageRating,
            ReviewCount = b.ReviewCount,
            IsVerified = b.IsVerified,
            IsFeatured = b.IsFeatured,
        };
    }

    private static PlaceRecommendationVm ToRecommendationVm(RecommendationItemResponse r) => new()
    {
        Kind = r.Kind.ToString(),
        Id = r.Id,
        Name = r.Name,
        Slug = r.Slug,
        BasePrice = r.BasePrice,
        Currency = r.Currency,
        AverageRating = r.AverageRating,
        IsFeatured = r.IsFeatured,
        IsPinned = r.IsPinned,
        BadgeText = r.BadgeText,
        IsBoosted = r.IsBoosted,
    };

    private static async Task<List<T>> SafeListAsync<T>(Func<Task<ApiResult<List<T>>>> call)
    {
        try
        {
            var result = await call();
            if (result is { IsSuccess: true, Data: { } data }) return data;
        }
        catch
        {
            // tolerate hydration failure
        }

        return [];
    }
}
