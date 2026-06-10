namespace YallaJo.Web.Areas.Public.Models.Shared;

/// <summary>
/// Canonical tour card used by every Public surface (home rails, tour grid,
/// package included-tours, search results). Rendered by
/// Areas/Public/Views/Shared/_TourCard.cshtml.
/// </summary>
public sealed class TourCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = string.Empty;
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }

    public decimal EffectivePrice => SalePrice ?? BasePrice;
    public bool HasDiscount => SalePrice is > 0 && SalePrice < BasePrice;
    public int DiscountPercent =>
        HasDiscount && BasePrice > 0
            ? (int)Math.Round((1 - (SalePrice!.Value / BasePrice)) * 100)
            : 0;
}
