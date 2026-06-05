using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class ToursFacade
{
    private readonly ToursApiClient _api;
    private readonly PlacesApiClient _placesApi;
    private readonly IApiAssetUrlResolver _assetResolver;

    // Sort tokens accepted by GET /api/v1/tours.
    private static readonly HashSet<string> AllowedSorts =
        new(StringComparer.OrdinalIgnoreCase)
        { "price_asc", "price_desc", "rating_desc", "popularity_desc", "newest" };

    public ToursFacade(ToursApiClient api, PlacesApiClient placesApi, IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _placesApi = placesApi;
        _assetResolver = assetResolver;
    }

    public static string NormalizeSort(string? sort)
        => sort is not null && AllowedSorts.Contains(sort) ? sort.ToLowerInvariant() : "popularity_desc";

    public async Task<ApiResult<TourGridVm>> GetGridAsync(
        int page, string? sort, string? query, Guid? placeId = null, CancellationToken ct = default)
    {
        const int pageSize = 12;
        var pageNumber = page < 1 ? 1 : page;
        var normalizedSort = NormalizeSort(sort);

        var toursTask = _api.GetToursAsync(pageNumber, pageSize, normalizedSort, placeId, ct);
        var categoriesTask = _api.GetCategoriesAsync(ct);
        // CP-3c: when a place filter is active, resolve its name for the heading
        // label. Tolerant — a failed/missing place just yields the generic label.
        var placeNameTask = SafePlaceNameAsync(placeId, ct);
        await Task.WhenAll(toursTask, categoriesTask, placeNameTask);

        var toursResult = await toursTask;
        var categoriesResult = await categoriesTask;

        if (!toursResult.IsSuccess || toursResult.Data is null)
            return ApiResult<TourGridVm>.Fail(toursResult.StatusCode, toursResult.Error ?? "Could not load tours.");

        var page0 = toursResult.Data;

        // Tour summaries expose no public image field and the attachment endpoint is
        // not anonymous-accessible, so cards use a deterministic theme placeholder
        // (temporary public image API gap — see PublicImagePlaceholder).
        var cards = page0.Items
            .Select(t => TourGridMapper.ToCardVm(t, PublicImagePlaceholder.ResolveTourImage(t.Id)))
            .ToList();

        var categories = categoriesResult is { IsSuccess: true, Data: { } cats }
            ? cats.Select(TourGridMapper.ToFilterVm).ToList()
            : new List<CategoryFilterVm>();

        return ApiResult<TourGridVm>.Ok(new TourGridVm
        {
            Tours = cards,
            Categories = categories,
            PageNumber = page0.PageNumber,
            PageSize = page0.PageSize,
            TotalCount = page0.TotalCount,
            TotalPages = page0.TotalPages,
            HasPreviousPage = page0.HasPreviousPage,
            HasNextPage = page0.HasNextPage,
            Query = query,
            Sort = normalizedSort,
            PlaceId = placeId,
            PlaceFilterName = placeNameTask.Result,
        });
    }

    public async Task<ApiResult<TourDetailVm>> GetDetailAsync(string slug, CancellationToken ct = default)
    {
        var detailResult = await _api.GetTourBySlugAsync(slug, ct);
        if (!detailResult.IsSuccess || detailResult.Data is null)
            return ApiResult<TourDetailVm>.Fail(detailResult.StatusCode, detailResult.Error ?? "Tour not found.");

        var d = detailResult.Data;

        var schedulesTask = SafeListAsync(() => _api.GetSchedulesAsync(d.Id, ct));
        var pricingTask = SafeListAsync(() => _api.GetPricingAsync(d.Id, ct));
        var waypointsTask = SafeListAsync(() => _api.GetWaypointsAsync(d.Id, ct));
        var guidesTask = SafeListAsync(() => _api.GetGuidesAsync(d.Id, ct));
        var imagesTask = SafeListAsync(() => _api.GetImagesAsync(d.Id, ct));
        var ratingTask = SafeRatingAsync(d.Id, ct);
        var reviewsTask = SafeReviewsAsync(d.Id, ct);
        var joinSlotsTask = SafeJoinSlotsAsync(d.Id, ct);
        var placeTask = SafePlaceAsync(d.PlaceId, ct);

        await Task.WhenAll(schedulesTask, pricingTask, waypointsTask, guidesTask, imagesTask, ratingTask, reviewsTask, joinSlotsTask, placeTask);

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
            Name = d.Name,
            Slug = d.Slug,
            Description = d.Description,
            ShortDescription = d.ShortDescription,
            Difficulty = d.Difficulty,
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
            PlaceId = d.PlaceId,
            ImageUrls = imageUrls,
            Waypoints = waypointsTask.Result
                .OrderBy(w => w.SortOrder)
                .Select(w => new TourWaypointVm
                {
                    Name = w.Name,
                    Description = w.Description,
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

        return ApiResult<TourDetailVm>.Ok(vm);
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
