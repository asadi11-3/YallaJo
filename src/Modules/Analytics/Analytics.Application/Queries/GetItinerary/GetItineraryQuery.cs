using Analytics.Application.Localization;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;

namespace Analytics.Application.Queries.GetItinerary;

public sealed record GetItineraryQuery(
    DateOnly FromDate,
    DateOnly ToDate,
    decimal? StartLatitude,
    decimal? StartLongitude,
    string? Interests,
    string? Language = null,
    bool HalalOnly = false,
    string? AcceptLanguage = null) : IQuery<ItineraryResponse>, ICacheableQuery
{
    public string CacheKey => $"analytics:itinerary:{FromDate:yyyyMMdd}:{ToDate:yyyyMMdd}:{StartLatitude}:{StartLongitude}:{Interests}:halal:{HalalOnly}";
    public TimeSpan? CacheDuration => TimeSpan.FromMinutes(30);
    public IReadOnlyList<string> Tags => ["analytics:itinerary"];
}

public sealed record ItineraryResponse(
    string ArcName,
    string? ArcDescription,
    int TotalDays,
    IReadOnlyList<ItineraryDay> Days,
    ItineraryLogistics Logistics);

public sealed record ItineraryDay(
    int DayNumber,
    string ClusterName,
    decimal Latitude,
    decimal Longitude,
    IReadOnlyList<ItineraryItem> Suggestions,
    double? DistanceFromPreviousKm);

public sealed record ItineraryItem(
    EntityType EntityKind,
    Guid EntityId,
    string Name,
    string? Slug,
    decimal? BasePrice,
    string? Currency,
    decimal AverageRating,
    int BookingCount,
    decimal Score,
    IReadOnlyList<string> Signals,
    IReadOnlyList<SignalLabelDto> SignalLabels);

public sealed record ItineraryLogistics(
    double TotalDistanceKm,
    string Summary);
