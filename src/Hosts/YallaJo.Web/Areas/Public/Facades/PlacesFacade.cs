using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Areas.Public.Models.Tours;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class PlacesFacade
{
    private const int PageSize = 20;

    // Small, fixed number of related tours shown on the place detail page.
    private const int RelatedToursCount = 4;

    private readonly PlacesApiClient _api;
    private readonly ToursApiClient _toursApi;
    private readonly IApiAssetUrlResolver _assetResolver;

    public PlacesFacade(
        PlacesApiClient api,
        ToursApiClient toursApi,
        IApiAssetUrlResolver assetResolver)
    {
        _api = api;
        _toursApi = toursApi;
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
        var vm = new PlaceDetailVm
        {
            Id                     = d.Id,
            PlaceId                = d.Id,
            Name                   = d.Name,
            Slug                   = d.Slug,
            ImageUrl               = PublicImagePlaceholder.ResolvePlaceImage(d.Id),
            PlaceType              = d.PlaceType,
            Latitude               = d.Latitude,
            Longitude              = d.Longitude,
            Description            = d.Description,
            Address                = d.Address,
            City                   = d.City,
            Country                = d.Country,
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
            MetaTitle              = d.MetaTitle,
        };

        // CP-3c: hydrate a small set of related (approved) tours for this place.
        // Tolerant — a failed/empty fetch leaves RelatedTours empty and never
        // breaks the place detail page.
        vm.RelatedTours = await GetRelatedToursAsync(d.Id, ct);

        // CP-4: real uploaded place images (tolerant; empty → placeholder used).
        vm.ImageUrls = await GetImageUrlsAsync(d.Id, ct);

        return ApiResult<PlaceDetailVm>.Ok(vm);
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
}
