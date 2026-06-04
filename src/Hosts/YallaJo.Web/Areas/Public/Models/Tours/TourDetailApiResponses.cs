namespace YallaJo.Web.Areas.Public.Models.Tours;

public sealed class TourDetailResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Slug { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ShortDescription { get; init; }
    public string? Difficulty { get; init; }
    public int DurationMinutes { get; init; }
    public int? MaxGroupSize { get; init; }
    public int? MinAge { get; init; }
    public decimal BasePrice { get; init; }
    public string Currency { get; init; } = "USD";
    public decimal? SalePrice { get; init; }
    public string? SalePriceCurrency { get; init; }
    public decimal? DiscountPercent { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? MeetingPointLatitude { get; init; }
    public double? MeetingPointLongitude { get; init; }
    public string? Status { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsInstantBooking { get; init; }
    public int? CancellationPolicyHours { get; init; }
    public bool IsChildFriendly { get; init; }
    public bool IsAccessible { get; init; }
    public int? AgeRestriction { get; init; }
    public string? MetaTitle { get; init; }
    public string? MetaDescription { get; init; }
    public Guid? PlaceId { get; init; }
}

public sealed class TourScheduleResponse
{
    public Guid Id { get; init; }
    public string DayOfWeek { get; init; } = string.Empty;
    public string? StartTime { get; init; }
    public string? EndTime { get; init; }
    public bool IsActive { get; init; }
}

public sealed class TourPricingTierResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string? ParticipantType { get; init; }
    public int? MinParticipants { get; init; }
    public int? MaxParticipants { get; init; }
    public bool IsActive { get; init; }
}

/// <summary>Mirrors the API TourImageDto from GET /api/v1/tours/{id}/images (public-safe).</summary>
public sealed class TourImageResponse
{
    public string Url { get; init; } = string.Empty;
    public string? ThumbnailUrl { get; init; }
    public int SortOrder { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class TourWaypointResponse
{
    public Guid Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public int SortOrder { get; init; }
    public int? DurationMinutes { get; init; }
    public string? WaypointType { get; init; }
}

public sealed class TourGuideResponse
{
    public Guid TourGuideId { get; init; }
    public bool IsPrimary { get; init; }
    public string DisplayName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
}

public sealed class RatingSummaryResponse
{
    public string? EntityType { get; init; }
    public Guid EntityId { get; init; }
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
}

public sealed class PublicReviewPageResponse
{
    public IReadOnlyList<ReviewResponse> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
}

public sealed class ReviewResponse
{
    public Guid Id { get; init; }
    public Guid UserId { get; init; }
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public DateTime? VisitDate { get; init; }
    public string? Status { get; init; }
    public bool IsVerifiedBooking { get; init; }
    public DateTime CreatedAt { get; init; }
    public int HelpfulVoteCount { get; init; }
}

public sealed class AvailabilityPageResponse
{
    public IReadOnlyList<AvailabilityDateGroupResponse> Items { get; init; } = [];
    public string? NextCursor { get; init; }
    public int? TotalCount { get; init; }
}

public sealed class AvailabilityDateGroupResponse
{
    public DateOnly Date { get; init; }
    public IReadOnlyList<AvailabilitySlotResponse> Slots { get; init; } = [];
}

public sealed class AvailabilitySlotResponse
{
    public Guid Id { get; init; }
    public string? StartTime { get; init; }
    public string? EndTime { get; init; }
    public int AvailableCount { get; init; }
    public Guid TourGuideId { get; init; }
}
