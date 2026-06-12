using YallaJo.Web.Infrastructure.Seo;

namespace YallaJo.Web.Areas.Public.Models.Tours;

public sealed class TourDetailVm
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
    public decimal? SalePrice { get; init; }
    public string Currency { get; init; } = "USD";
    public double AverageRating { get; init; }
    public int ReviewCount { get; init; }
    public int BookingCount { get; init; }
    public bool IsFeatured { get; init; }
    public bool IsInstantBooking { get; init; }
    public bool IsChildFriendly { get; init; }
    public bool IsAccessible { get; init; }
    public int? CancellationPolicyHours { get; init; }

    // Geographic data for the detail map (Mapbox). Mirrors the API TourDetailResponse;
    // the meeting point takes priority over the general tour location when both exist.
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public double? MeetingPointLatitude { get; init; }
    public double? MeetingPointLongitude { get; init; }

    public Guid? PlaceId { get; init; }
    public string? PlaceName { get; set; }
    public string? PlaceCity { get; set; }
    public string? PlaceCountry { get; set; }
    public bool PlaceLookupFailed { get; set; }

    public SeoContent? Seo { get; set; }

    public string? PlaceDisplay =>
        PlaceId is not { } id
            ? null
            : PlaceLookupFailed || string.IsNullOrWhiteSpace(PlaceName)
                ? Infrastructure.Api.PlaceDisplayFormatter.UnresolvedLabel(id)
                : Infrastructure.Api.PlaceDisplayFormatter.Format(PlaceName, PlaceCity, PlaceCountry);

    public IReadOnlyList<string> ImageUrls { get; init; } = [];
    public IReadOnlyList<TourWaypointVm> Waypoints { get; init; } = [];
    public IReadOnlyList<TourScheduleVm> Schedules { get; init; } = [];
    public IReadOnlyList<TourPricingTierVm> PricingTiers { get; init; } = [];
    public IReadOnlyList<TourGuideVm> Guides { get; init; } = [];
    public IReadOnlyList<TourReviewVm> Reviews { get; init; } = [];
    public IReadOnlyList<JoinSlotVm> JoinSlots { get; init; } = [];

    public bool HasJoinSlots => JoinSlots.Count > 0;

    public decimal EffectivePrice => SalePrice ?? BasePrice;
    public bool HasDiscount => SalePrice is { } s && s < BasePrice;
    public int DiscountPercent => HasDiscount && BasePrice > 0
        ? (int)Math.Round((1 - (SalePrice!.Value / BasePrice)) * 100)
        : 0;
    public string DurationLabel
    {
        get
        {
            if (DurationMinutes <= 0) return "Flexible";
            var h = DurationMinutes / 60;
            var m = DurationMinutes % 60;
            return h > 0 ? (m > 0 ? $"{h}h {m}m" : $"{h}h") : $"{m}m";
        }
    }
}

public sealed class TourWaypointVm
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public double? Latitude { get; init; }
    public double? Longitude { get; init; }
    public int SortOrder { get; init; }
    public int? DurationMinutes { get; init; }
    public string? WaypointType { get; init; }
}

public sealed class TourScheduleVm
{
    public string DayOfWeek { get; init; } = string.Empty;
    public string? StartTime { get; init; }
    public string? EndTime { get; init; }
}

public sealed class TourPricingTierVm
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public decimal Price { get; init; }
    public string Currency { get; init; } = "USD";
    public string? ParticipantType { get; init; }
    public int? MinParticipants { get; init; }
    public int? MaxParticipants { get; init; }
}

public sealed class TourGuideVm
{
    public string DisplayName { get; init; } = string.Empty;
    public string? AvatarUrl { get; init; }
    public bool IsPrimary { get; init; }
}

public sealed class TourReviewVm
{
    public int Rating { get; init; }
    public string? Title { get; init; }
    public string? Content { get; init; }
    public DateTime CreatedAt { get; init; }
    public bool IsVerifiedBooking { get; init; }
    public int HelpfulVoteCount { get; init; }
}

public sealed class JoinSlotVm
{
    public Guid SlotId { get; init; }
    public string Label { get; init; } = string.Empty;
}

public sealed record SubmitJoinRequestBody(Guid TourBookingId, Guid AvailabilitySlotId, int ParticipantCount, string? Message);
