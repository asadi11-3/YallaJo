namespace ContentCore.Presentation.Endpoints.PromoBlock.Models;

/// <summary>
/// Inline-edit payload for a promotional placement. The placement key is taken
/// from the route; this body carries the editable content fields only.
/// </summary>
public sealed record UpdatePromoBlockRequest(
    string Title,
    string? Description = null,
    string? ButtonText = null,
    string? ButtonUrl = null,
    string? BadgeText = null,
    string? IconName = null,
    bool IsActive = true,
    int SortOrder = 0,
    DateTimeOffset? StartsAt = null,
    DateTimeOffset? EndsAt = null);
