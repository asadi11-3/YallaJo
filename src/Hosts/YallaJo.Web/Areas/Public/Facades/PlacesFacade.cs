using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Places;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

public sealed class PlacesFacade
{
    private const int PageSize = 20;

    private readonly PlacesApiClient _api;

    public PlacesFacade(PlacesApiClient api) => _api = api;

    public async Task<ApiResult<PlacesGridVm>> GetGridAsync(int page, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;

        var result = await _api.ListAsync(pageNumber, PageSize, ct);
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

        return ApiResult<PlaceDetailVm>.Ok(vm);
    }
}
