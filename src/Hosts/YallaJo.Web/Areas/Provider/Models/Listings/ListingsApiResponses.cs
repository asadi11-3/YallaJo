namespace YallaJo.Web.Areas.Provider.Models.Listings;

public sealed class ListMyToursResponse
{
    public IReadOnlyList<ListingTourResponse> Items { get; init; } = [];
    public int Total { get; init; }
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalPages { get; init; }
}

public sealed class ListingTourResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal? SalePrice { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public string? Status { get; init; }
    public DateTime CreatedAt { get; init; }
}
