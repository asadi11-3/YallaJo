using PromoBlockEntity = ContentCore.Domain.Entities.PromoBlock;

namespace ContentCore.Application.Queries.PromoBlock.Common;

/// <summary>
/// Read model for a promotional placement block rendered on the site
/// (e.g. the My Profile right-rail and bottom banner).
/// </summary>
public sealed class PromoBlockDto
{
    public Guid Id { get; init; }
    public string PlacementKey { get; init; } = string.Empty;
    public string Title { get; init; } = string.Empty;
    public string? Description { get; init; }
    public string? ImageUrl { get; init; }
    public string? ButtonText { get; init; }
    public string? ButtonUrl { get; init; }
    public string? BadgeText { get; init; }
    public string? IconName { get; init; }
    public bool IsActive { get; init; }
    public int SortOrder { get; init; }
    public DateTimeOffset? StartsAt { get; init; }
    public DateTimeOffset? EndsAt { get; init; }

    public PromoBlockDto()
    {
    }

    public static PromoBlockDto From(PromoBlockEntity entity) => new()
    {
        Id = entity.Id,
        PlacementKey = entity.PlacementKey,
        Title = entity.Title,
        Description = entity.Description,
        ImageUrl = entity.ImageUrl,
        ButtonText = entity.ButtonText,
        ButtonUrl = entity.ButtonUrl,
        BadgeText = entity.BadgeText,
        IconName = entity.IconName,
        IsActive = entity.IsActive,
        SortOrder = entity.SortOrder,
        StartsAt = entity.StartsAt,
        EndsAt = entity.EndsAt,
    };
}
