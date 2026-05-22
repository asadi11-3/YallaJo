using Analytics.Application.Localization;
using Analytics.Domain.Enums;

namespace Analytics.Application.Models;

public sealed record RecommendationItemDto(
    EntityType Kind,
    Guid Id,
    string Name,
    string Slug,
    decimal? BasePrice,
    string? Currency,
    decimal AverageRating,
    int BookingCount,
    decimal Score,
    IReadOnlyList<string> Signals,
    IReadOnlyList<SignalLabelDto> SignalLabels,
    bool IsFeatured,
    bool IsPinned = false,
    string? BadgeText = null,
    bool IsBoosted = false);
