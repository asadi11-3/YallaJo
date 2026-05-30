using Analytics.Application.Interfaces.Repositories;
using Analytics.Domain.Entities;

namespace Analytics.Application.Models;

internal static class EditorialPinInjector
{
    /// <summary>
    /// Injects editorial pins at their specified positions into the item list, shifting existing items down.
    /// </summary>
    public static async Task<List<RecommendationItemDto>> InjectPins(
        List<RecommendationItemDto> items,
        IReadOnlyList<EditorialPin> pins,
        IEntityAttributeSnapshotRepository snapshotRepository,
        int limit,
        CancellationToken ct)
    {
        if (pins.Count == 0)
            return items;

        foreach (var pin in pins.OrderBy(p => p.Position))
        {
            // Skip if already in the list
            if (items.Any(i => i.Id == pin.EntityId && i.Kind == pin.EntityKind))
                continue;

            var snapshot = await snapshotRepository.GetByEntityAsync(pin.EntityKind, pin.EntityId, ct).ConfigureAwait(false);
            if (snapshot is null)
                continue;

            var pinnedItem = RecommendationMapping.ToPinnedDto(snapshot, pin.BadgeText);
            var insertAt = Math.Min(pin.Position - 1, items.Count); // 1-based → 0-based
            items.Insert(insertAt, pinnedItem);
        }

        // Trim to limit
        if (items.Count > limit)
            items.RemoveRange(limit, items.Count - limit);

        return items;
    }
}
