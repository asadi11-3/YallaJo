using System.Text.Json;
using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Enums;
using YallaJo.SharedKernel.Application.Abstractions.Messaging;
using YallaJo.SharedKernel.Domain.Abstractions.Results;

namespace Analytics.Application.Queries.GetItinerary;

internal sealed class GetItineraryQueryHandler(
    ITripArcRepository tripArcRepository,
    IEntityAttributeSnapshotRepository snapshotRepository) : IQueryHandler<GetItineraryQuery, ItineraryResponse>
{
    public async Task<Result<ItineraryResponse>> Handle(GetItineraryQuery request, CancellationToken ct)
    {
        var days = request.ToDate.DayNumber - request.FromDate.DayNumber + 1;
        if (days < 1)
            return Result.Failure<ItineraryResponse>(new Error("Itinerary.InvalidDates", "ToDate must be >= FromDate"));

        var arc = await tripArcRepository.GetBestMatchAsync(days, request.Interests, ct);
        if (arc is null)
            return Result.Failure<ItineraryResponse>(new Error("Itinerary.NoArcs", "No trip arc templates available"));

        var clusters = ParseDayClusters(arc.DayClustersJson);
        if (clusters.Count == 0)
            return Result.Failure<ItineraryResponse>(new Error("Itinerary.EmptyArc", "Trip arc has no day clusters"));

        // Load all active snapshots for scoring
        var allTours = await snapshotRepository.GetActiveByKindAsync(EntityType.Tour, ct);
        var allBusinesses = await snapshotRepository.GetActiveByKindAsync(EntityType.Business, ct);
        var allPlaces = await snapshotRepository.GetActiveByKindAsync(EntityType.Place, ct);
        var allSnapshots = allTours.Concat(allBusinesses).Concat(allPlaces).ToList();

        var effectiveHalalOnly = request.HalalOnly || IsArabicLocale(request.AcceptLanguage);

        var itineraryDays = new List<ItineraryDay>();
        double totalDistanceKm = 0;
        decimal? prevLat = request.StartLatitude;
        decimal? prevLng = request.StartLongitude;

        // Spread clusters across user's days
        for (var dayIndex = 0; dayIndex < days; dayIndex++)
        {
            var clusterIndex = Math.Min(dayIndex, clusters.Count - 1);
            var cluster = clusters[clusterIndex];

            // Find nearby snapshots for this cluster
            var nearbySnapshots = allSnapshots
                .Where(s => s.LocationLatitude is not null && s.LocationLongitude is not null)
                .Where(s => !effectiveHalalOnly || s.IsHalal == true)
                .Where(s => DistanceKm(cluster.Latitude, cluster.Longitude, s.LocationLatitude!.Value, s.LocationLongitude!.Value) <= 50)
                .ToList();

            // Create a "virtual source" at the cluster center for scoring
            var suggestions = new List<ItineraryItem>();
            if (nearbySnapshots.Count > 0)
            {
                // Sort by distance and take top scoring
                var sorted = nearbySnapshots
                    .Select(s => new
                    {
                        Snapshot = s,
                        Distance = DistanceKm(cluster.Latitude, cluster.Longitude, s.LocationLatitude!.Value, s.LocationLongitude!.Value)
                    })
                    .OrderBy(x => x.Distance)
                    .Take(20)
                    .ToList();

                foreach (var item in sorted.Take(5))
                {
                    var distScore = 1.0m - (decimal)(item.Distance / 50.0);
                    var ratingScore = Math.Clamp(item.Snapshot.AverageRating / 5m, 0m, 1m);
                    var score = 0.4m * distScore + 0.3m * ratingScore + 0.3m * Math.Clamp((decimal)(Math.Log10(item.Snapshot.BookingCount + 1d) / 4d), 0m, 1m);
                    var signals = new List<string>();
                    if (item.Distance <= 5) signals.Add("nearby");
                    if (ratingScore >= 0.8m) signals.Add("high-rating");
                    if (item.Snapshot.BookingCount >= 10) signals.Add("popular");

                    suggestions.Add(new ItineraryItem(
                        item.Snapshot.EntityKind,
                        item.Snapshot.EntityId,
                        item.Snapshot.Name,
                        item.Snapshot.Slug,
                        item.Snapshot.BasePriceAmount,
                        item.Snapshot.BasePriceCurrency,
                        item.Snapshot.AverageRating,
                        item.Snapshot.BookingCount,
                        Math.Round(score, 4),
                        signals,
                        Localization.SignalLabels.Resolve(signals)));
                }
            }

            // Distance from previous day
            double? distFromPrev = null;
            if (prevLat is not null && prevLng is not null)
            {
                distFromPrev = DistanceKm(prevLat.Value, prevLng.Value, cluster.Latitude, cluster.Longitude);
                totalDistanceKm += distFromPrev.Value;
            }

            itineraryDays.Add(new ItineraryDay(
                dayIndex + 1,
                cluster.Name,
                cluster.Latitude,
                cluster.Longitude,
                suggestions,
                distFromPrev.HasValue ? Math.Round(distFromPrev.Value, 1) : null));

            prevLat = cluster.Latitude;
            prevLng = cluster.Longitude;
        }

        var logistics = new ItineraryLogistics(
            Math.Round(totalDistanceKm, 1),
            $"{days}-day trip covering approximately {Math.Round(totalDistanceKm, 0)} km across {clusters.Count} regions");

        return Result.Success(new ItineraryResponse(
            arc.Name,
            arc.Description,
            days,
            itineraryDays,
            logistics));
    }

    private static bool IsArabicLocale(string? acceptLanguage)
        => !string.IsNullOrWhiteSpace(acceptLanguage) && acceptLanguage.StartsWith("ar", StringComparison.OrdinalIgnoreCase);

    private static double DistanceKm(decimal lat1, decimal lng1, decimal lat2, decimal lng2)
    {
        const double R = 6371.0;
        var dLat = ((double)(lat2 - lat1)) * Math.PI / 180;
        var dLng = ((double)(lng2 - lng1)) * Math.PI / 180;
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos((double)lat1 * Math.PI / 180) * Math.Cos((double)lat2 * Math.PI / 180) *
                Math.Sin(dLng / 2) * Math.Sin(dLng / 2);
        return R * 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
    }

    private static IReadOnlyList<DayCluster> ParseDayClusters(string json)
    {
        try
        {
            return JsonSerializer.Deserialize<DayCluster[]>(json, new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
        }
        catch (JsonException) { return []; }
    }

    private sealed record DayCluster(int Day, string Name, decimal Latitude, decimal Longitude);
}
