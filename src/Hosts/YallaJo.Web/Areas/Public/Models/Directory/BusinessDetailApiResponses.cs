namespace YallaJo.Web.Areas.Public.Models.Directory;

public sealed class BusinessDetailResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? BusinessType { get; init; }
    public Guid? PlaceId { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public string? Address { get; init; }
    public string? City { get; init; }
    public string? Country { get; init; }
    public string? PostalCode { get; init; }
    public string? Phone { get; init; }
    public string? Email { get; init; }
    public string? Website { get; init; }
    public decimal AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public bool IsVerified { get; init; }
    public bool IsFeatured { get; init; }
    public string? Status { get; init; }
    public int ServiceItemCount { get; init; }
    public int StaffCount { get; init; }
    public int AmenityCount { get; init; }
    public IReadOnlyList<BusinessHoursResponse> BusinessHours { get; init; } = [];
}

public sealed class BusinessHoursResponse
{
    public Guid Id { get; init; }
    public string? DayOfWeek { get; init; }
    public string? OpenTime { get; init; }
    public string? CloseTime { get; init; }
    public bool IsClosed { get; init; }
}

public sealed class BusinessAmenityResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Icon { get; init; }
    public int SortOrder { get; init; }
}

public sealed class ServiceItemResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public decimal Price { get; init; }
    public int? DurationMinutes { get; init; }
    public string? Currency { get; init; }
}

public sealed class AccessibilityFeatureResponse
{
    public Guid Id { get; init; }
    public string? FeatureType { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public bool IsAvailable { get; init; }
}
