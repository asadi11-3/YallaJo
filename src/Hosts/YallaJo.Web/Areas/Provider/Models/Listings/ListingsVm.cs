namespace YallaJo.Web.Areas.Provider.Models.Listings;

public sealed class ListingsVm
{
    public IReadOnlyList<ListingCardVm> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; } = 1;
    public int PageSize { get; init; } = 10;
    public int TotalPages { get; init; }
    public string? StatusFilter { get; init; }

    public bool HasResults => Items.Count > 0;
    public bool HasPreviousPage => Page > 1;
    public bool HasNextPage => Page < TotalPages;

    public static readonly IReadOnlyList<string> StatusOptions =
        new[] { "Draft", "Pending", "Approved", "Rejected", "Suspended", "Archived" };
}

public sealed class ListingCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string Status { get; init; } = "Draft";
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public DateTime CreatedAt { get; init; }

    public decimal EffectivePrice => SalePrice ?? BasePrice;
    public bool HasDiscount => SalePrice.HasValue && SalePrice.Value < BasePrice && BasePrice > 0;
}
