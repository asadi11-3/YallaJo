namespace YallaJo.Web.Areas.Public.Models.Home;

/// <summary>A tour card shown on the homepage (featured / popular rails).</summary>
public sealed class HomeTourCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public decimal BasePrice { get; init; }
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }

    public decimal EffectivePrice => SalePrice ?? BasePrice;
    public bool HasDiscount => SalePrice is { } sale && sale < BasePrice && BasePrice > 0;
    public int DiscountPercent =>
        HasDiscount ? (int)Math.Round((BasePrice - SalePrice!.Value) / BasePrice * 100m) : 0;
}

/// <summary>A category tile shown on the homepage.</summary>
public sealed class HomeCategoryVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Icon { get; init; }
}

/// <summary>A place card shown on the homepage (popular destinations rail).</summary>
public sealed class HomePlaceCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsFeatured { get; init; }

    public string Location =>
        string.Join(", ", new[] { City, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>A business card shown on the homepage (popular businesses rail).</summary>
public sealed class HomeBusinessCardVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? ImageUrl { get; init; }
    public string? BusinessType { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsFeatured { get; init; }

    public string Location =>
        string.Join(", ", new[] { City, Country }.Where(s => !string.IsNullOrWhiteSpace(s)));
}

/// <summary>The complete homepage view model.</summary>
public sealed class HomeVm
{
    public IReadOnlyList<HomeTourCardVm> FeaturedTours { get; init; } = [];
    public IReadOnlyList<HomeTourCardVm> PopularTours { get; init; } = [];
    public IReadOnlyList<HomePlaceCardVm> PopularPlaces { get; init; } = [];
    public IReadOnlyList<HomeBusinessCardVm> PopularBusinesses { get; init; } = [];
    public IReadOnlyList<HomeCategoryVm> Categories { get; init; } = [];

    public bool HasFeatured => FeaturedTours.Count > 0;
    public bool HasPopular => PopularTours.Count > 0;
    public bool HasPopularPlaces => PopularPlaces.Count > 0;
    public bool HasPopularBusinesses => PopularBusinesses.Count > 0;
    public bool HasCategories => Categories.Count > 0;
}
