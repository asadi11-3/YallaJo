using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Packages;
using YallaJo.Web.Areas.Public.Models.Shared;
using YallaJo.Web.Infrastructure.Api.Contracts;
using YallaJo.Web.Services;

namespace YallaJo.Web.Areas.Public.Facades;

/// <summary>Maps public package endpoints into view models.</summary>
public sealed class PackagesFacade(PackagesApiClient api, IApiAssetUrlResolver assetResolver)
{
    private const int PageSize = 12;

    public async Task<ApiResult<PackagesGridVm>> GetGridAsync(
        int page,
        string? sort = null,
        decimal? minPrice = null,
        decimal? maxPrice = null,
        CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        var sortKey = NormalizeSort(sort);
        var result = await api.GetPackagesAsync(pageNumber, PageSize, sortKey, minPrice, maxPrice, ct);
        if (result is not { IsSuccess: true, Data: { } data })
        {
            return ApiResult<PackagesGridVm>.Fail(result.StatusCode, result.Error ?? "Could not load packages.");
        }

        var cards = data.Items.Select(p => new PackageCardVm
        {
            Id = p.Id,
            Name = p.Name,
            Description = p.Description,
            PriceAmount = p.PriceAmount,
            Currency = p.Currency,
            IncludedTourCount = p.IncludedTourCount,
            MaxParticipants = p.MaxParticipants,
            ValidFrom = p.ValidFrom,
            ValidTo = p.ValidTo,
            CoverImageUrl = assetResolver.Resolve(p.CoverImageUrl),
        }).ToList();

        return ApiResult<PackagesGridVm>.Ok(new PackagesGridVm
        {
            Packages = cards,
            PageNumber = data.PageNumber,
            PageSize = data.PageSize,
            TotalCount = data.TotalCount,
            TotalPages = data.TotalPages,
            HasPreviousPage = data.HasPreviousPage,
            HasNextPage = data.HasNextPage,
            Sort = sortKey,
            MinPrice = minPrice,
            MaxPrice = maxPrice,
        });
    }

    /// <summary>Maps toolbar sort keys to the values the backend accepts; unknown values fall back to Newest (null).</summary>
    private static string? NormalizeSort(string? sort) => sort switch
    {
        "price_asc" or "price_desc" or "validity_ending_soon" => sort,
        _ => null,
    };

    public async Task<ApiResult<PackageDetailVm>> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var result = await api.GetPackageAsync(id, ct);
        if (result is not { IsSuccess: true, Data: { } d })
        {
            return ApiResult<PackageDetailVm>.Fail(result.StatusCode, result.Error ?? "Package not found.");
        }

        var includedTours = d.IncludedTours.Select(t => new TourCardVm
        {
            Id = t.Id,
            Name = t.Name,
            Slug = t.Slug,
            ImageUrl = PublicImagePlaceholder.ResolveTourImage(t.Id),
            BasePrice = t.BasePrice,
            SalePrice = t.SalePrice,
            Currency = string.IsNullOrWhiteSpace(t.Currency) ? "USD" : t.Currency,
            AverageRating = t.AverageRating,
            ReviewCount = t.ReviewCount,
            BookingCount = t.BookingCount,
            IsFeatured = t.IsFeatured,
        }).ToList();

        var inclusions = d.Inclusions
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Description)
            .Where(text => !string.IsNullOrWhiteSpace(text))
            .ToList();

        return ApiResult<PackageDetailVm>.Ok(new PackageDetailVm
        {
            Id = d.Id,
            Name = d.Name,
            Description = d.Description,
            PriceAmount = d.PriceAmount,
            Currency = d.Currency,
            MaxParticipants = d.MaxParticipants,
            ValidFrom = d.ValidFrom,
            ValidTo = d.ValidTo,
            CoverImageUrl = assetResolver.Resolve(d.CoverImageUrl),
            IncludedTours = includedTours,
            Inclusions = inclusions,
        });
    }
}
