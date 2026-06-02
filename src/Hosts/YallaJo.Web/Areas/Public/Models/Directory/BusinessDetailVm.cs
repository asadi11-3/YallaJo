namespace YallaJo.Web.Areas.Public.Models.Directory;

public sealed class BusinessDetailVm
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BusinessType { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsFeatured { get; init; }

    public IReadOnlyList<string> ImageUrls { get; init; } = [];
    public IReadOnlyList<BusinessHoursVm> Hours { get; init; } = [];
    public IReadOnlyList<BusinessAmenityVm> Amenities { get; init; } = [];
    public IReadOnlyList<BusinessServiceVm> Services { get; init; } = [];
    public IReadOnlyList<AccessibilityFeatureVm> AccessibilityFeatures { get; init; } = [];

    public string? Location => string.Join(", ", new[] { City, Country }.Where(p => !string.IsNullOrWhiteSpace(p)));
}

public sealed class BusinessHoursVm
{
    public string? DayOfWeek { get; init; }
    public string? OpenTime { get; init; }
    public string? CloseTime { get; init; }
    public bool IsClosed { get; init; }
}

public sealed class BusinessAmenityVm
{
    public string Name { get; init; } = string.Empty;
    public string? Icon { get; init; }
}

public sealed class BusinessServiceVm
{
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Currency { get; init; }
}

public sealed class AccessibilityFeatureVm
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsAvailable { get; init; }
}
