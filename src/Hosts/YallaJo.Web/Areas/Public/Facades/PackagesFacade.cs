using YallaJo.Web.Areas.Public.ApiClients;
using YallaJo.Web.Areas.Public.Helpers;
using YallaJo.Web.Areas.Public.Models.Home;
using YallaJo.Web.Areas.Public.Models.Packages;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Public.Facades;

/// <summary>Maps public package endpoints into view models.</summary>
public sealed class PackagesFacade(PackagesApiClient api)
{
    private const int PageSize = 12;

    public async Task<ApiResult<PackagesGridVm>> GetGridAsync(int page, CancellationToken ct = default)
    {
        var pageNumber = page < 1 ? 1 : page;
        var result = await api.GetPackagesAsync(pageNumber, PageSize, ct);
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
            ValidFrom = p.ValidFrom,
            ValidTo = p.ValidTo,
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
        });
    }

    public async Task<ApiResult<PackageDetailVm>> GetDetailAsync(Guid id, CancellationToken ct = default)
    {
        var result = await api.GetPackageAsync(id, ct);
        if (result is not { IsSuccess: true, Data: { } d })
        {
            return ApiResult<PackageDetailVm>.Fail(result.StatusCode, result.Error ?? "Package not found.");
        }

        var includedTours = d.IncludedTours.Select(t => new HomeTourCardVm
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
            IncludedTours = includedTours,
            Inclusions = inclusions,
        });
    }
}
