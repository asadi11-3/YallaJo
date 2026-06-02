namespace YallaJo.Web.Areas.Accounts.Models.Wishlist;

public sealed class WishlistAttachmentResponse
{
    public Guid Id { get; init; }

    public string Url { get; init; } = string.Empty;

    public string? ThumbnailUrl { get; init; }

    public int SortOrder { get; init; }
}

public sealed class TourLookupResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public decimal BasePrice { get; init; }

    public decimal? SalePrice { get; init; }

    public string Currency { get; init; } = string.Empty;

    public decimal? AverageRating { get; init; }
}

public sealed class PlaceLookupResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? City { get; init; }

    public string? Country { get; init; }

    public decimal? AverageRating { get; init; }
}

public sealed class BusinessLookupResponse
{
    public Guid Id { get; init; }

    public string Name { get; init; } = string.Empty;

    public string? City { get; init; }

    public string? Country { get; init; }

    public decimal? AverageRating { get; init; }
}

public sealed class TourGuideLookupResponse
{
    public Guid Id { get; init; }

    public string DisplayName { get; init; } = string.Empty;

    public string? AvatarUrl { get; init; }

    public decimal? AverageRating { get; init; }
}

public sealed class BlogLookupResponse
{
    public Guid Id { get; init; }

    public string Title { get; init; } = string.Empty;
}
