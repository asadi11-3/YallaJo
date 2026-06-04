using YallaJo.Web.Areas.Provider.ApiClients;
using YallaJo.Web.Areas.Provider.Models.Listings;
using YallaJo.Web.Infrastructure.Api.Contracts;

namespace YallaJo.Web.Areas.Provider.Facades;

public sealed class ListingsFacade
{
    private const int PageSize = 10;

    private readonly ListingsApiClient _api;

    public ListingsFacade(ListingsApiClient api) => _api = api;

    public async Task<ApiResult<ListingsVm>> GetListingsAsync(
        string? status, int page, CancellationToken ct = default)
    {
        if (page < 1) page = 1;

        var statusFilter = NormalizeStatus(status);

        var result = await _api.GetMyToursAsync(page, PageSize, statusFilter, sort: null, ct);

        if (result.IsUnauthorized)
            return ApiResult<ListingsVm>.ForceSignOut();

        if (!result.IsSuccess || result.Data is null)
            return ApiResult<ListingsVm>.Fail(result.StatusCode, result.Error ?? "Could not load your listings.");

        var data = result.Data;

        var items = data.Items
            .Select(t => new ListingCardVm
            {
                Id = t.Id,
                Name = t.Name,
                Slug = t.Slug,
                Status = string.IsNullOrWhiteSpace(t.Status) ? "Draft" : t.Status,
                BasePrice = t.BasePrice,
                SalePrice = t.SalePrice,
                Currency = t.Currency,
                AverageRating = t.AverageRating,
                ReviewCount = t.ReviewCount,
                BookingCount = t.BookingCount,
                IsFeatured = t.IsFeatured,
                CreatedAt = t.CreatedAt,
            })
            .ToList();

        var vm = new ListingsVm
        {
            Items = items,
            Total = data.Total,
            Page = data.Page <= 0 ? page : data.Page,
            PageSize = data.PageSize <= 0 ? PageSize : data.PageSize,
            TotalPages = data.TotalPages,
            StatusFilter = statusFilter,
        };

        return ApiResult<ListingsVm>.Ok(vm);
    }

    public async Task<ApiResult> DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var result = await _api.DeleteAsync(id, ct);

        if (result.IsUnauthorized)
            return ApiResult.ForceSignOut();

        if (result.IsSuccess || result.IsNotFound)
            return ApiResult.Ok();

        return ApiResult.Fail(result.StatusCode, result.Error ?? "Could not delete the listing.");
    }

    private static string? NormalizeStatus(string? status)
    {
        if (string.IsNullOrWhiteSpace(status))
            return null;

        return ListingsVm.StatusOptions.FirstOrDefault(
            s => string.Equals(s, status, StringComparison.OrdinalIgnoreCase));
    }
}
