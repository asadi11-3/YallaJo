using System.Text.Json;
using Analytics.Application.Localization;
using Analytics.Domain.Entities;

namespace Analytics.Application.Models;

internal static class RecommendationMapping
{
    public static RecommendationItemDto ToDto(EntityAttributeSnapshot snapshot, decimal score, string? signalsJson, bool isBoosted = false)
    {
        var signals = ParseSignals(signalsJson);
        return new(
            snapshot.EntityKind,
            snapshot.EntityId,
            snapshot.Name,
            snapshot.Slug ?? string.Empty,
            snapshot.BasePriceAmount,
            snapshot.BasePriceCurrency,
            snapshot.AverageRating,
            snapshot.BookingCount,
            score,
            signals,
            SignalLabels.Resolve(signals),
            snapshot.IsFeatured,
            IsBoosted: isBoosted);
    }

    public static RecommendationItemDto ToPinnedDto(EntityAttributeSnapshot snapshot, string? badgeText)
    {
        IReadOnlyList<string> signals = ["pinned"];
        return new(
            snapshot.EntityKind,
            snapshot.EntityId,
            snapshot.Name,
            snapshot.Slug ?? string.Empty,
            snapshot.BasePriceAmount,
            snapshot.BasePriceCurrency,
            snapshot.AverageRating,
            snapshot.BookingCount,
            Score: 1m,
            signals,
            SignalLabels.Resolve(signals),
            snapshot.IsFeatured,
            IsPinned: true,
            BadgeText: badgeText ?? "Editor's Choice");
    }

    public static RecommendationItemDto ToPopularityDto(EntityAttributeSnapshot snapshot)
    {
        IReadOnlyList<string> signals = ["popular"];
        return new(
            snapshot.EntityKind,
            snapshot.EntityId,
            snapshot.Name,
            snapshot.Slug ?? string.Empty,
            snapshot.BasePriceAmount,
            snapshot.BasePriceCurrency,
            snapshot.AverageRating,
            snapshot.BookingCount,
            0m,
            signals,
            SignalLabels.Resolve(signals),
            snapshot.IsFeatured);
    }

    public static bool HasAnyCategory(EntityAttributeSnapshot snapshot, ISet<Guid> categoryIds)
    {
        if (categoryIds.Count == 0)
            return true;

        return ParseCategoryIds(snapshot.CategoryIdsJson).Any(categoryIds.Contains);
    }

    private static IReadOnlyList<string> ParseSignals(string? signalsJson)
    {
        if (string.IsNullOrWhiteSpace(signalsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<string>>(signalsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }

    private static IReadOnlyList<Guid> ParseCategoryIds(string? categoryIdsJson)
    {
        if (string.IsNullOrWhiteSpace(categoryIdsJson))
            return [];

        try
        {
            return JsonSerializer.Deserialize<IReadOnlyList<Guid>>(categoryIdsJson) ?? [];
        }
        catch (JsonException)
        {
            return [];
        }
    }
}
