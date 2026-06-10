using YallaJo.Web.Areas.Public.Models.Shared;

namespace YallaJo.Web.Areas.Public.Models.Tours;

/// <summary>Pure projection helpers for the tour grid (no I/O).</summary>
public static class TourGridMapper
{
    public static CategoryFilterVm ToFilterVm(CategoryResponse c) => new()
    {
        Id = c.Id,
        Name = c.Name,
        Slug = c.Slug,
    };

    public static TourCardVm ToCardVm(TourSummaryResponse t, string? imageUrl) => new()
    {
        Id = t.Id,
        Name = t.Name,
        Slug = t.Slug,
        ImageUrl = imageUrl,
        BasePrice = t.BasePrice,
        SalePrice = t.SalePrice,
        Currency = t.Currency,
        AverageRating = t.AverageRating,
        ReviewCount = t.ReviewCount,
        BookingCount = t.BookingCount,
        IsFeatured = t.IsFeatured,
    };
}
