using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Search;
using YallaJo.Web.Areas.Public.Models.Shared;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Areas.Public.Translations;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Infrastructure.Seo;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class ToursFacade
{
    private readonly ToursApiClient _api;
    private readonly SearchApiClient _searchApi;
    private readonly PlacesApiClient _placesApi;
    private readonly SeoApiClient _seo;
    private readonly TranslationsApiClient _translations;
    private readonly IApiAssetUrlResolver _assetResolver;

    // Sort tokens accepted by the tour grid. "relevance" only makes sense on the
    // search path (GET /api/v1/tours/search); the browse path maps it back to
    // popularity_desc because GET /api/v1/tours has no relevance ordering.
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase)
        { "price_asc", "price_desc", "rating_desc", "popularity_desc", "newest", "relevance" };

    // Web sort token → backend SearchSort enum name (case-insensitive minimal-API binding).
    private static readonly Dictionary<string, string> SearchSortMap =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["relevance"] = "Relevance",
            ["price_asc"] = "PriceAsc",
            ["price_desc"] = "PriceDesc",
            ["rating_desc"] = "RatingDesc",
            ["popularity_desc"] = "PopularityDesc",
            ["newest"] = "Newest"
        };

    public ToursFacade(ToursApiClient api, SearchApiClient searchApi, PlacesApiClient placesApi, SeoApiClient seo, TranslationsApiClient translations, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _searchApi = searchApi;
        _placesApi = placesApi;
        _seo = seo;
        _translations = translations;
        _assetResolver = assetResolver;
    }

    public static string NormalizeSort(string? sort)
        => sort is not null && AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "popularity_desc";

    public async Task<ApiResult<TourGridVm>> GetGridAsync(
        int page, string? sort, string? query, Guid? placeId = null, TourFilterVm? filters = null, CancellationToken ct = default)
    {
        const int pageSize = 12;
        var pageNumber = page < 1 ? 1 : page;
        filters ??= new TourFilterVm();

        // Phase 4.2: a text query or any server-side filter routes through the search
        // endpoint (16-param GET /api/v1/tours/search); plain browsing stays on the
        // cheaper GET /api/v1/tours. Default sort becomes relevance when searching.
        var useSearch = !string.IsNullOrWhiteSpace(query) || filters.HasAny;
        var normalizedSort = sort is null && !string.IsNullOrWhiteSpace(query)
            ? "relevance"
            : NormalizeSort(sort);

        var categoriesTask = _api.GetCategoriesAsync(ct);
        // CP-3c: when a place filter is active, resolve its name for the heading
        // label. Tolerant — a failed/missing place just yields the generic label.
        var placeNameTask = SafePlaceNameAsync(placeId, ct);

        List<TourCardVm> cards;
        int totalCount, totalPages, respPage, respPageSize;
        bool hasPrev, hasNext;

        if (useSearch)
        {
            var searchTask = _searchApi.SearchToursAsync(query, pageNumber, pageSize, placeId, filters, SearchSortMap[normalizedSort], ct);
            await Task.WhenAll(searchTask, categoriesTask, placeNameTask);

            var searchResult = await searchTask;
            if (!searchResult.IsSuccess || searchResult.Data is null)
                return ApiResult<TourGridVm>.Fail(searchResult.StatusCode, searchResult.Error ?? "Could not load tours.");

            var sp = searchResult.Data;
            cards = sp.Items.Select(MapSearchCard).ToList();
            totalCount = sp.TotalCount;
            totalPages = sp.TotalPages;
            respPage = sp.PageNumber;
            respPageSize = sp.PageSize;
            // The search payload omits has-previous/has-next — derive them from the
            // resolved page position (matches PaginatedResult<T> semantics on the browse path).
            hasPrev = sp.PageNumber > 1;
            hasNext = sp.PageNumber < sp.TotalPages;
        }
        else
        {
            // Browse path has no "relevance" ordering — degrade to popularity.
            var browseSort = normalizedSort == "relevance" ? "popularity_desc" : normalizedSort;
            var toursTask = _api.GetToursAsync(pageNumber, pageSize, browseSort, placeId, ct);
            await Task.WhenAll(toursTask, categoriesTask, placeNameTask);

            var toursResult = await toursTask;
            if (!toursResult.IsSuccess || toursResult.Data is null)
                return ApiResult<TourGridVm>.Fail(toursResult.StatusCode, toursResult.Error ?? "Could not load tours.");

            var page0 = toursResult.Data;

            // Cards show the real primary tour image (relative /uploads path resolved
            // to absolute via the asset resolver). The backend batch-loads it per page
            // (no N+1). Fall back to a deterministic theme placeholder when a tour has
            // no image (PrimaryImageUrl null) — see PublicImagePlaceholder.
            cards = page0.Items
                .Select(t => TourGridMapper.ToCardVm(t, ResolveCardImage(t.PrimaryImageUrl, t.Id)))
                .ToList();
            totalCount = page0.TotalCount;
            totalPages = page0.TotalPages;
            respPage = page0.PageNumber;
            respPageSize = page0.PageSize;
            hasPrev = page0.HasPreviousPage;
            hasNext = page0.HasNextPage;
        }

        var categoriesResult = await categoriesTask;
        var categories = categoriesResult is { IsSuccess: true, Data: { } cats }
            ? cats.Select(TourGridMapper.ToFilterVm).ToList()
            : new List<CategoryFilterVm>();

        return ApiResult<TourGridVm>.Ok(new TourGridVm
        {
            Tours = cards,
            Categories = categories,
            PageNumber = respPage,
            PageSize = respPageSize,
            TotalCount = totalCount,
            TotalPages = totalPages,
            HasPreviousPage = hasPrev,
            HasNextPage = hasNext,
            Query = query,
            Sort = normalizedSort,
            Filters = filters,
            PlaceId = placeId,
            PlaceFilterName = placeNameTask.Result,
        });
    }

    /// <summary>Search items expose the same card fields as browse summaries (see SearchFacade.MapItem precedent).</summary>
    private TourCardVm MapSearchCard(TourSearchItemResponse item) => new()
    {
        Id = item.Id,
        Name = item.Name,
        Slug = item.Slug,
        ImageUrl = ResolveCardImage(item.PrimaryImageUrl, item.Id),
        BasePrice = item.BasePrice,
        SalePrice = item.SalePrice,
        Currency = string.IsNullOrWhiteSpace(item.Currency) ? "USD" : item.Currency,
        AverageRating = item.AverageRating,
        ReviewCount = item.ReviewCount,
        BookingCount = item.BookingCount,
        IsFeatured = item.IsFeatured
    };

    /// <summary>
    /// Resolves a card's image: the real primary image (relative /uploads path made
    /// absolute) when present, otherwise a deterministic theme placeholder keyed by id.
    /// Mirrors the tour-detail image resolution (GetDetailAsync).
    /// </summary>
    private string ResolveCardImage(string? primaryImageUrl, Guid tourId)
    {
        if (!string.IsNullOrWhiteSpace(primaryImageUrl))
        {
            var resolved = _assetResolver.Resolve(primaryImageUrl);
            if (!string.IsNullOrWhiteSpace(resolved))
                return resolved!;
        }

        return PublicImagePlaceholder.ResolveTourImage(tourId);
    }

    public async Task<ApiResult<TourDetailVm>> GetDetailAsync(string slug, CancellationToken ct = default)
    {
        var detailResult = await _api.GetTourBySlugAsync(slug, ct);
        if (!detailResult.IsSuccess || detailResult.Data is null)
            return ApiResult<TourDetailVm>.Fail(detailResult.StatusCode, detailResult.Error ?? "Tour not found.");

        var d = detailResult.Data;
        var languageCode = TranslationOverlay.ActiveLanguageCode;
        var translationsTask = TranslationOverlay.GetApprovedAsync(_translations, "Tour", d.Id, languageCode, ct);

        var schedulesTask = SafeListAsync(() => _api.GetSchedulesAsync(d.Id, ct));
        var pricingTask = SafeListAsync(() => _api.GetPricingAsync(d.Id, ct));
        var waypointsTask = SafeListAsync(() => _api.GetWaypointsAsync(d.Id, ct));
        var guidesTask = SafeListAsync(() => _api.GetGuidesAsync(d.Id, ct));
        var imagesTask = SafeListAsync(() => _api.GetImagesAsync(d.Id, ct));
        var ratingTask = SafeRatingAsync(d.Id, ct);
        var reviewsTask = SafeReviewsAsync(d.Id, ct);
        var joinSlotsTask = SafeJoinSlotsAsync(d.Id, ct);
        var placeTask = SafePlaceAsync(d.PlaceId, ct);

        await Task.WhenAll(schedulesTask, pricingTask, waypointsTask, guidesTask, imagesTask, ratingTask, reviewsTask, joinSlotsTask, placeTask, translationsTask);
        var translations = translationsTask.Result;

        // Real uploaded images come from the anonymous GET /api/v1/tours/{id}/images
        // endpoint (approved-only, primary-first). Relative /uploads URLs are resolved
        // to absolute via the asset resolver. Fall back to a deterministic placeholder
        // when the approved tour has no images (or the fetch failed gracefully).
        var imageUrls = imagesTask.Result
            .Select(i => _assetResolver.Resolve(i.Url))
            .Where(u => !string.IsNullOrWhiteSpace(u))
            .Select(u => u!)
            .ToList();

        if (imageUrls.Count == 0)
            imageUrls = new List<string> { PublicImagePlaceholder.ResolveTourImage(d.Id) };

        var rating = ratingTask.Result;
        var avgRating = rating?.AverageRating ?? d.AverageRating;
        var reviewCount = rating?.ReviewCount ?? d.ReviewCount;

        var vm = new TourDetailVm
        {
            Id = d.Id,
            Name = TranslationOverlay.Apply(translations, "Name", d.Name, languageCode) ?? d.Name,
            Slug = d.Slug,
            Description = TranslationOverlay.Apply(translations, "Description", d.Description, languageCode),
            ShortDescription = TranslationOverlay.Apply(translations, "ShortDescription", d.ShortDescription, languageCode),
            Difficulty = TranslationOverlay.Apply(translations, "Difficulty", d.Difficulty, languageCode) ?? d.Difficulty,
            DurationMinutes = d.DurationMinutes,
            MaxGroupSize = d.MaxGroupSize,
            MinAge = d.MinAge,
            BasePrice = d.BasePrice,
            SalePrice = d.SalePrice,
            Currency = d.Currency,
            AverageRating = avgRating,
            ReviewCount = reviewCount,
            BookingCount = d.BookingCount,
            IsFeatured = d.IsFeatured,
            IsInstantBooking = d.IsInstantBooking,
            IsChildFriendly = d.IsChildFriendly,
            IsAccessible = d.IsAccessible,
            CancellationPolicyHours = d.CancellationPolicyHours,
            Latitude = d.Latitude,
            Longitude = d.Longitude,
            MeetingPointLatitude = d.MeetingPointLatitude,
            MeetingPointLongitude = d.MeetingPointLongitude,
            PlaceId = d.PlaceId,
            ImageUrls = imageUrls,
            Waypoints = waypointsTask.Result
                .OrderBy(w => w.SortOrder)
                .Select(w => new TourWaypointVm
                {
                    Name = w.Name,
                    Description = w.Description,
                    Latitude = w.Latitude,
                    Longitude = w.Longitude,
                    SortOrder = w.SortOrder,
                    DurationMinutes = w.DurationMinutes,
                    WaypointType = w.WaypointType,
                })
                .ToList(),
            Schedules = schedulesTask.Result
                .Select(s => new TourScheduleVm
                {
                    DayOfWeek = s.DayOfWeek,
                    StartTime = s.StartTime,
                    EndTime = s.EndTime,
                })
                .ToList(),
            PricingTiers = pricingTask.Result
                .Select(p => new TourPricingTierVm
                {
                    Name = p.Name,
                    Description = p.Description,
                    Price = p.Price,
                    Currency = p.Currency,
                    ParticipantType = p.ParticipantType,
                    MinParticipants = p.MinParticipants,
                    MaxParticipants = p.MaxParticipants,
                })
                .ToList(),
            Guides = guidesTask.Result
                .OrderByDescending(g => g.IsPrimary)
                .Select(g => new TourGuideVm
                {
                    DisplayName = g.DisplayName,
                    AvatarUrl = _assetResolver.Resolve(g.AvatarUrl),
                    IsPrimary = g.IsPrimary,
                })
                .ToList(),
            Reviews = reviewsTask.Result
                .Select(r => new TourReviewVm
                {
                    Rating = r.Rating,
                    Title = r.Title,
                    Content = r.Content,
                    CreatedAt = r.CreatedAt,
                    IsVerifiedBooking = r.IsVerifiedBooking,
                    HelpfulVoteCount = r.HelpfulVoteCount,
                })
                .ToList(),
            JoinSlots = joinSlotsTask.Result,
        };

        if (d.PlaceId is not null)
        {
            if (placeTask.Result is { } place)
            {
                vm.PlaceName = place.Name;
                vm.PlaceCity = place.City;
                vm.PlaceCountry = place.Country;
            }
            else
            {
                vm.PlaceLookupFailed = true;
            }
        }

        vm.Seo = TranslationOverlay.ApplySeo(
            await GetSeoAsync(d.Id, d.ShortDescription ?? d.Description, ct), translations, languageCode,
            fallbackTitle: vm.Name,
            fallbackDescription: vm.ShortDescription ?? vm.Description);

        return ApiResult<TourDetailVm>.Ok(vm);
    }

    // SEO metadata + FAQ for the tour detail page (SeoEntityType.Tour). Best-effort:
    // never fails the page — seeds from the tour itself, overrides from /seo/* when present.
    private async Task<SeoContent> GetSeoAsync(Guid tourId, string? fallbackDescription, CancellationToken ct)
    {
        string? metaTitle = null;
        var metaDescription = fallbackDescription;
        string? canonical = null;
        string? ogImage = PublicImagePlaceholder.ResolveTourImage(tourId);
        IReadOnlyList<SeoFaqItem> faqs = [];

        try
        {
            var metaTask = _seo.GetMetadataAsync(SeoEntityType.Tour, tourId, ct);
            var faqTask = _seo.GetFaqAsync(SeoEntityType.Tour, tourId, ct);
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
            // tolerate SEO hydration failure
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

    public async Task<ApiResult> SubmitJoinRequestAsync(SubmitJoinRequestBody body, CancellationToken ct = default)
    {
        ApiResult result;
        try
        {
            result = await _api.SubmitJoinRequestAsync(body, ct);
        }
        catch
        {
            return ApiResult.Fail(500, "Could not send your request. Please try again.");
        }

        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();
        if (result.IsSuccess)
            return ApiResult.Ok(result.StatusCode);
        if (result.IsValidationError && result.ValidationErrors is not null)
            return ApiResult.ValidationFail(result.StatusCode, result.ValidationErrors);

        // Conflict surfaces the API message verbatim (e.g. "Only N spots remain.", booking not joinable, already requested).
        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not send your request.");
    }

    private async Task<List<JoinSlotVm>> SafeJoinSlotsAsync(Guid tourId, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetAvailabilityAsync(tourId, ct);
            if (result is { IsSuccess: true, Data: { } page })
            {
                return page.Items
                    .SelectMany(g => g.Slots.Select(s => new JoinSlotVm
                    {
                        SlotId = s.Id,
                        Label = $"{g.Date:ddd, d MMM yyyy} {s.StartTime}-{s.EndTime} ({s.AvailableCount} left)",
                    }))
                    .ToList();
            }
        }
        catch
        {
            // tolerate availability hydration failure
        }

        return new List<JoinSlotVm>();
    }

    private async Task<PlaceLookupResponse?> SafePlaceAsync(Guid? placeId, CancellationToken ct)
    {
        if (placeId is not { } id) return null;

        try
        {
            var result = await _placesApi.GetByIdAsync(id, ct);
            if (result is { IsSuccess: true, Data: { } data })
                return data;
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            throw;
        }
        catch
        {
            // tolerate place hydration failure
        }

        return null;
    }

    // CP-3c: resolve only the place name for the /tours filter heading. Reuses
    // the tolerant SafePlaceAsync (returns null on missing/deleted/failed lookup).
    private async Task<string?> SafePlaceNameAsync(Guid? placeId, CancellationToken ct)
    {
        var place = await SafePlaceAsync(placeId, ct);
        return string.IsNullOrWhiteSpace(place?.Name) ? null : place!.Name;
    }

    private static async Task<List<T>> SafeListAsync<T>(Func<Task<ApiResult<List<T>>>> call)
    {
        try
        {
            var result = await call();
            if (result is { IsSuccess: true, Data: { } data })
                return data;
        }
        catch
        {
            // tolerate per-call failure
        }

        return new List<T>();
    }

    private async Task<RatingSummaryResponse?> SafeRatingAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetRatingSummaryAsync(id, ct);
            if (result is { IsSuccess: true, Data: { } data })
                return data;
        }
        catch
        {
            // tolerate failure
        }

        return null;
    }

    private async Task<List<ReviewResponse>> SafeReviewsAsync(Guid id, CancellationToken ct)
    {
        try
        {
            var result = await _api.GetReviewsAsync(id, 1, 10, ct);
            if (result is { IsSuccess: true, Data: { } data })
                return data.Items.ToList();
        }
        catch
        {
            // tolerate failure
        }

        return new List<ReviewResponse>();
    }
}
